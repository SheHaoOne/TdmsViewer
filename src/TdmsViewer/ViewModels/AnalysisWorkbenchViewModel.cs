using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisWorkbenchViewModel : ObservableObject
{
    private readonly Func<Task<AnalysisInputContext?>> _loadAnalysisInputAsync;
    private readonly Func<bool> _canAnalyze;
    private readonly Action<AnalysisReportModel> _onReportReady;
    private readonly AnalysisStepRegistry _registry = new();
    private readonly PipelineRunner _runner;
    private AnalysisPlan _currentPlan = AnalysisPlan.CreateDefault();

    public AnalysisWorkbenchViewModel(
        Func<Task<AnalysisInputContext?>> loadAnalysisInputAsync,
        Func<bool> canAnalyze,
        Action<AnalysisReportModel> onReportReady)
    {
        _loadAnalysisInputAsync = loadAnalysisInputAsync;
        _canAnalyze = canAnalyze;
        _onReportReady = onReportReady;
        _runner = new PipelineRunner(_registry);

        foreach (var definition in _registry.GetDefinitions())
        {
            var item = new AnalysisStepItem
            {
                StepType = definition.StepType,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Category = definition.Category,
                IsEnabled = _currentPlan.Steps.Any(s =>
                    string.Equals(s.StepType, definition.StepType, StringComparison.OrdinalIgnoreCase) && s.Enabled)
            };
            item.InitializeParameters();
            AvailableSteps.Add(item);
        }

        ApplyPlan(_currentPlan, selectFirstStep: true);
    }

    [ObservableProperty]
    private string _planName = AnalysisPlan.CreateDefault().Name;

    [ObservableProperty]
    private string _statusMessage = "勾选分析步骤后点击「运行分析」";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _progressText;

    [ObservableProperty]
    private string? _channelSummary;

    [ObservableProperty]
    private AnalysisStepItem? _selectedStep;

    public bool CanEditParameters => SelectedStep?.HasParameters == true;

    public bool ShowNoParametersMessage => SelectedStep != null && !SelectedStep.HasParameters;

    partial void OnSelectedStepChanged(AnalysisStepItem? value)
    {
        OnPropertyChanged(nameof(CanEditParameters));
        OnPropertyChanged(nameof(ShowNoParametersMessage));
        ResetStepParametersCommand.NotifyCanExecuteChanged();
    }

    public ObservableCollection<AnalysisStepItem> AvailableSteps { get; } = new();

    public void RefreshChannelSummary(AnalysisTargetDescription? target)
    {
        ChannelSummary = target == null
            ? "请先选择通道"
            : target.SourceCount <= 1
                ? $"{target.FileName} / {target.GroupName} / {target.ChannelName} · {target.SampleRateHz:N0} Hz · {target.SampleCount:N0} 点"
                : $"{target.ChannelName} · {target.SourceCount} 个文件 · {target.GroupName} · {target.SampleRateHz:N0} Hz · {target.SampleCount:N0} 点";
    }

    [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
    private async Task RunAnalysisAsync()
    {
        if (!_canAnalyze())
        {
            MessageBox.Show("请先选择通道并加载数据。", "数据分析", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!NvhLicenseService.TryLoad())
        {
            MessageBox.Show(
                NvhLicenseService.LastError ?? "NVH 许可证加载失败。",
                "数据分析",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!TryValidateEnabledSteps(out var validationError))
        {
            MessageBox.Show(validationError, "参数校验", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var plan = BuildPlanFromUi();
        IsRunning = true;
        StatusMessage = "正在读取数据并分析…";
        ProgressText = null;

        try
        {
            var progress = new Progress<PipelineProgress>(p =>
            {
                ProgressText = $"({p.CompletedSteps + 1}/{p.TotalSteps}) {p.CurrentStepName}";
            });

            var report = await Task.Run(async () =>
            {
                var input = await _loadAnalysisInputAsync().ConfigureAwait(false);
                if (input == null)
                    throw new InvalidOperationException("无法读取通道数据。");

                return await _runner.RunAsync(plan, input, progress).ConfigureAwait(false);
            });

            StatusMessage = $"分析完成，共 {report.Cards.Count} 张图表";
            _onReportReady(report);
        }
        catch (Exception ex)
        {
            StatusMessage = "分析失败";
            MessageBox.Show($"分析失败：{ex.Message}", "数据分析", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            ProgressText = null;
            RunAnalysisCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "分析方案 (*.tdms-analysis.json)|*.tdms-analysis.json",
            FileName = $"{SanitizeFileName(PlanName)}.tdms-analysis.json",
            Title = "保存分析方案"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await AnalysisPlanSerializer.SaveAsync(BuildPlanFromUi(), dialog.FileName);
            StatusMessage = $"方案已保存：{dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "分析方案", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadPlanAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "分析方案 (*.tdms-analysis.json)|*.tdms-analysis.json|JSON (*.json)|*.json",
            Title = "加载分析方案"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var plan = await AnalysisPlanSerializer.LoadAsync(dialog.FileName);
            ApplyPlan(plan);
            StatusMessage = $"已加载方案：{plan.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败：{ex.Message}", "分析方案", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ResetPlan()
    {
        ApplyPlan(AnalysisPlan.CreateDefault());
        StatusMessage = "已恢复默认方案";
    }

    [RelayCommand(CanExecute = nameof(CanResetStepParameters))]
    private void ResetStepParameters()
    {
        if (SelectedStep == null)
            return;

        SelectedStep.ApplyParameterValues(null);
        StatusMessage = $"已恢复「{SelectedStep.DisplayName}」默认参数";
    }

    private bool CanResetStepParameters() => SelectedStep?.HasParameters == true;

    private bool TryValidateEnabledSteps(out string error)
    {
        foreach (var step in AvailableSteps.Where(s => s.IsEnabled && s.HasParameters))
        {
            var stepError = step.ValidateParameters();
            if (stepError != null)
            {
                error = stepError;
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private bool CanRunAnalysis() => !IsRunning && _canAnalyze();

    partial void OnIsRunningChanged(bool value) => RunAnalysisCommand.NotifyCanExecuteChanged();

    public void NotifyCanAnalyzeChanged() => RunAnalysisCommand.NotifyCanExecuteChanged();

    private AnalysisPlan BuildPlanFromUi()
    {
        var currentSteps = _currentPlan.Steps.ToDictionary(s => s.StepType, StringComparer.OrdinalIgnoreCase);
        var defaultSteps = AnalysisPlan.CreateDefault().Steps.ToDictionary(s => s.StepType, StringComparer.OrdinalIgnoreCase);
        var steps = new List<AnalysisPlanStep>();

        foreach (var item in AvailableSteps)
        {
            currentSteps.TryGetValue(item.StepType, out var current);
            defaultSteps.TryGetValue(item.StepType, out var defaults);

            steps.Add(new AnalysisPlanStep
            {
                Id = current?.Id ?? defaults?.Id ?? item.StepType.ToLowerInvariant(),
                StepType = item.StepType,
                Enabled = item.IsEnabled,
                Parameters = item.HasParameters
                    ? item.ToParameterDictionary()
                    : ResolveParameters(current, defaults)
            });
        }

        return new AnalysisPlan
        {
            Name = PlanName,
            Steps = steps
        };
    }

    private static IReadOnlyDictionary<string, object?> ResolveParameters(
        AnalysisPlanStep? current,
        AnalysisPlanStep? defaults)
    {
        if (current?.Parameters is { Count: > 0 })
            return current.Parameters;

        if (defaults?.Parameters is { Count: > 0 })
            return defaults.Parameters;

        return new Dictionary<string, object?>();
    }

    private void ApplyPlan(AnalysisPlan plan, bool selectFirstStep = false)
    {
        _currentPlan = plan;
        PlanName = plan.Name;

        foreach (var item in AvailableSteps)
        {
            var match = plan.Steps.FirstOrDefault(s =>
                string.Equals(s.StepType, item.StepType, StringComparison.OrdinalIgnoreCase));
            item.IsEnabled = match?.Enabled ?? false;
            item.ApplyParameterValues(match?.Parameters);
        }

        if (selectFirstStep || SelectedStep == null)
            SelectedStep = AvailableSteps.FirstOrDefault(s => s.HasParameters) ?? AvailableSteps.FirstOrDefault();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(name) ? "analysis-plan" : name.Trim();
    }
}

public sealed class AnalysisTargetDescription
{
    public required string FileName { get; init; }
    public required string GroupName { get; init; }
    public required string ChannelName { get; init; }
    public required double SampleRateHz { get; init; }
    public required int SampleCount { get; init; }
    public int SourceCount { get; init; } = 1;
}
