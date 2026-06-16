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
    private readonly Func<AnalysisInputContext?> _getAnalysisInput;
    private readonly Func<bool> _canAnalyze;
    private readonly Action<AnalysisReportModel> _onReportReady;
    private readonly AnalysisStepRegistry _registry = new();
    private readonly PipelineRunner _runner;
    private AnalysisPlan _currentPlan = AnalysisPlan.CreateDefault();

    public AnalysisWorkbenchViewModel(
        Func<AnalysisInputContext?> getAnalysisInput,
        Func<bool> canAnalyze,
        Action<AnalysisReportModel> onReportReady)
    {
        _getAnalysisInput = getAnalysisInput;
        _canAnalyze = canAnalyze;
        _onReportReady = onReportReady;
        _runner = new PipelineRunner(_registry);

        foreach (var definition in _registry.GetDefinitions())
        {
            AvailableSteps.Add(new AnalysisStepItem
            {
                StepType = definition.StepType,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Category = definition.Category,
                IsEnabled = _currentPlan.Steps.Any(s =>
                    string.Equals(s.StepType, definition.StepType, StringComparison.OrdinalIgnoreCase) && s.Enabled)
            });
        }
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

    public ObservableCollection<AnalysisStepItem> AvailableSteps { get; } = new();

    public void RefreshChannelSummary(AnalysisInputContext? input)
    {
        ChannelSummary = input == null
            ? "请先选择通道并单击文件名加载数据"
            : $"{input.FileName} / {input.GroupName} / {input.ChannelName} · {input.SampleRateHz:N0} Hz · {input.Samples.Length:N0} 点";
    }

    [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
    private async Task RunAnalysisAsync()
    {
        var input = _getAnalysisInput();
        if (input == null)
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

        var plan = BuildPlanFromUi();
        IsRunning = true;
        StatusMessage = "正在分析…";
        ProgressText = null;

        try
        {
            var progress = new Progress<PipelineProgress>(p =>
            {
                ProgressText = $"({p.CompletedSteps + 1}/{p.TotalSteps}) {p.CurrentStepName}";
            });

            var report = await Task.Run(
                () => _runner.RunAsync(plan, input, progress),
                CancellationToken.None);

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

    private bool CanRunAnalysis() => !IsRunning && _canAnalyze();

    partial void OnIsRunningChanged(bool value) => RunAnalysisCommand.NotifyCanExecuteChanged();

    public void NotifyCanAnalyzeChanged() => RunAnalysisCommand.NotifyCanExecuteChanged();

    private AnalysisPlan BuildPlanFromUi()
    {
        var defaultSteps = AnalysisPlan.CreateDefault().Steps.ToDictionary(s => s.StepType, StringComparer.OrdinalIgnoreCase);
        var steps = new List<AnalysisPlanStep>();

        foreach (var item in AvailableSteps)
        {
            defaultSteps.TryGetValue(item.StepType, out var template);
            steps.Add(new AnalysisPlanStep
            {
                Id = template?.Id ?? item.StepType.ToLowerInvariant(),
                StepType = item.StepType,
                Enabled = item.IsEnabled,
                Parameters = template?.Parameters ?? new Dictionary<string, object?>()
            });
        }

        return new AnalysisPlan
        {
            Name = PlanName,
            Steps = steps
        };
    }

    private void ApplyPlan(AnalysisPlan plan)
    {
        _currentPlan = plan;
        PlanName = plan.Name;

        foreach (var item in AvailableSteps)
        {
            var match = plan.Steps.FirstOrDefault(s =>
                string.Equals(s.StepType, item.StepType, StringComparison.OrdinalIgnoreCase));
            item.IsEnabled = match?.Enabled ?? false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(name) ? "analysis-plan" : name.Trim();
    }
}
