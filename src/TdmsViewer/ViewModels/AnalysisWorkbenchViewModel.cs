using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Models;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisWorkbenchViewModel : ObservableObject
{
    private readonly Func<ChannelSourceRef?> _getActiveSource;
    private readonly Func<MergedChannelInfo?> _getSelectedChannel;
    private readonly Func<double[]?> _getCurrentChannelData;
    private readonly Func<bool> _canAnalyze;
    private readonly Func<ChannelSourceRef, double[]> _readChannelData;
    private readonly Action<AnalysisWorkbenchResult> _onResultReady;
    private readonly AnalysisStepRegistry _registry = new();
    private readonly PipelineRunner _runner;

    public AnalysisWorkbenchViewModel(
        Func<ChannelSourceRef?> getActiveSource,
        Func<MergedChannelInfo?> getSelectedChannel,
        Func<double[]?> getCurrentChannelData,
        Func<bool> canAnalyze,
        Func<ChannelSourceRef, double[]> readChannelData,
        Action<AnalysisWorkbenchResult> onResultReady)
    {
        _getActiveSource = getActiveSource;
        _getSelectedChannel = getSelectedChannel;
        _getCurrentChannelData = getCurrentChannelData;
        _canAnalyze = canAnalyze;
        _readChannelData = readChannelData;
        _onResultReady = onResultReady;
        _runner = new PipelineRunner(_registry);

        foreach (var definition in _registry.GetDefinitions())
        {
            AvailableSteps.Add(new AnalysisStepItem
            {
                StepType = definition.StepType,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Category = definition.Category,
                IsEnabled = IsStepEnabledByDefault(definition.StepType)
            });
        }
    }

    [ObservableProperty]
    private string _planName = "NVH 声学分析";

    [ObservableProperty]
    private string _statusMessage = "勾选分析步骤后点击「运行分析」";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _progressText;

    [ObservableProperty]
    private bool _isBatchMode;

    public ObservableCollection<AnalysisStepItem> AvailableSteps { get; } = new();

    [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
    private async Task RunAnalysisAsync()
    {
        if (IsBatchMode)
            await RunBatchAnalysisAsync();
        else
            await RunSingleAnalysisAsync();
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
            var document = CreatePlanDocument();
            await AnalysisPlanSerializer.SaveAsync(document, dialog.FileName);
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
            var document = await AnalysisPlanSerializer.LoadAsync(dialog.FileName);
            ApplyPlanDocument(document);
            StatusMessage = $"已加载方案：{document.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败：{ex.Message}", "分析方案", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RunSingleAnalysisAsync()
    {
        if (AvailableSteps.All(s => !s.IsEnabled))
        {
            MessageBox.Show("请至少启用一个分析步骤。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var source = _getActiveSource();
        if (source == null)
        {
            MessageBox.Show("请先在左侧选择通道，并单击文件名。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryValidateSampleRate(source.Channel.SampleRateHz, out var sampleRate))
            return;

        try
        {
            IsRunning = true;
            var data = await LoadChannelDataAsync(source).ConfigureAwait(true);
            if (data == null)
                return;

            var input = CreateInputContext(source, data, sampleRate);
            var plan = BuildPlan();
            var report = await ExecutePlanAsync(plan, input).ConfigureAwait(true);

            _onResultReady(new AnalysisWorkbenchResult
            {
                PrimaryReport = report,
                AllReports = [report]
            });

            StatusMessage = $"分析完成 — {report.Blocks.Count} 个报表块";
            ProgressText = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"分析失败：{ex.Message}", "分析编排", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "分析失败";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RunBatchAnalysisAsync()
    {
        if (AvailableSteps.All(s => !s.IsEnabled))
        {
            MessageBox.Show("请至少启用一个分析步骤。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var merged = _getSelectedChannel();
        if (merged == null)
        {
            MessageBox.Show("请先在左侧选择通道。", "批量分析", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sources = merged.Sources
            .Where(s => s.Channel.SampleRateHz is > 0)
            .ToList();

        if (sources.Count == 0)
        {
            MessageBox.Show("当前通道在所有文件中均缺少有效采样率。", "批量分析", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsRunning = true;
            var plan = BuildPlan();
            var reports = new List<AnalysisReport>(sources.Count);

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                StatusMessage = $"批量分析 ({i + 1}/{sources.Count})：{source.FileName}…";
                ProgressText = source.FileName;

                var data = await Task.Run(() => _readChannelData(source)).ConfigureAwait(true);
                if (data.Length == 0)
                    continue;

                var input = CreateInputContext(source, data, source.Channel.SampleRateHz!.Value);
                var report = await ExecutePlanAsync(plan, input, i, sources.Count).ConfigureAwait(true);
                reports.Add(report);
            }

            if (reports.Count == 0)
            {
                MessageBox.Show("没有生成任何分析报表。", "批量分析", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var primary = reports.Count == 1
                ? reports[0]
                : BatchReportBuilder.BuildCompareReport(reports, plan.Name, merged.DisplayName);

            _onResultReady(new AnalysisWorkbenchResult
            {
                PrimaryReport = primary,
                AllReports = reports
            });

            StatusMessage = reports.Count == 1
                ? $"分析完成 — {reports[0].Blocks.Count} 个报表块"
                : $"批量分析完成 — {reports.Count} 个文件";
            ProgressText = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"批量分析失败：{ex.Message}", "分析编排", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "批量分析失败";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<AnalysisReport> ExecutePlanAsync(
        AnalysisPlan plan,
        AnalysisInputContext input,
        int batchIndex = 0,
        int batchTotal = 1)
    {
        var progress = new Progress<PipelineProgress>(p =>
        {
            var prefix = batchTotal > 1 ? $"[{batchIndex + 1}/{batchTotal}] " : string.Empty;
            ProgressText = $"{prefix}{p.CurrentStepName} ({p.CompletedSteps}/{p.TotalSteps})";
            StatusMessage = $"{prefix}正在分析：{p.CurrentStepName}…";
        });

        return await Task.Run(() => _runner.RunAsync(plan, input, progress)).ConfigureAwait(true);
    }

    private async Task<double[]?> LoadChannelDataAsync(ChannelSourceRef source)
    {
        IsRunning = true;
        StatusMessage = "正在准备数据…";

        var data = _getCurrentChannelData();
        var active = _getActiveSource();
        if (data == null || data.Length == 0 ||
            active == null ||
            !string.Equals(active.FilePath, source.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = $"正在读取 {source.FileName}…";
            data = await Task.Run(() => _readChannelData(source)).ConfigureAwait(true);
        }

        if (data.Length == 0)
        {
            MessageBox.Show("当前通道没有可分析的数据。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        return data;
    }

    private static AnalysisInputContext CreateInputContext(ChannelSourceRef source, double[] data, double sampleRate) =>
        new()
        {
            FilePath = source.FilePath,
            FileName = source.FileName,
            GroupName = source.Channel.GroupName,
            ChannelName = source.Channel.ChannelName,
            Samples = data,
            SampleRateHz = sampleRate
        };

    private bool TryValidateSampleRate(double? sampleRateHz, out double sampleRate)
    {
        sampleRate = sampleRateHz ?? 0;
        if (sampleRateHz is > 0)
            return true;

        MessageBox.Show(
            "当前通道缺少有效采样率（wf_increment 或 NI_SampleRate）。\nNVH 分析需要采样率大于 0。",
            "分析编排",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private bool CanRunAnalysis() => !IsRunning && _canAnalyze();

    partial void OnIsRunningChanged(bool value) => RunAnalysisCommand.NotifyCanExecuteChanged();

    public void NotifySessionChanged() => RunAnalysisCommand.NotifyCanExecuteChanged();

    private AnalysisPlan BuildPlan()
    {
        var enabledSteps = AvailableSteps.Where(s => s.IsEnabled).ToList();
        if (enabledSteps.Count == 0)
            throw new InvalidOperationException("请至少启用一个分析步骤。");

        return new AnalysisPlan
        {
            Name = string.IsNullOrWhiteSpace(PlanName) ? "NVH 声学分析" : PlanName.Trim(),
            DashboardTemplateId = "nvh-acoustic-light",
            Steps = enabledSteps
                .Select(s => new AnalysisPlanStep
                {
                    StepType = s.StepType,
                    Id = AnalysisStepIds.GetBlockId(s.StepType),
                    Enabled = true
                })
                .ToList()
        };
    }

    private AnalysisPlanDocument CreatePlanDocument() => new()
    {
        Name = string.IsNullOrWhiteSpace(PlanName) ? "NVH 声学分析" : PlanName.Trim(),
        DashboardTemplateId = "nvh-acoustic-light",
        BatchMode = IsBatchMode,
        Steps = AvailableSteps
            .Select(s => new AnalysisPlanStepDocument
            {
                StepType = s.StepType,
                Enabled = s.IsEnabled
            })
            .ToList()
    };

    private void ApplyPlanDocument(AnalysisPlanDocument document)
    {
        PlanName = document.Name;
        IsBatchMode = document.BatchMode;

        var stepMap = AvailableSteps.ToDictionary(s => s.StepType, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<AnalysisStepItem>();

        foreach (var stepDoc in document.Steps)
        {
            if (!stepMap.TryGetValue(stepDoc.StepType, out var item))
                continue;

            item.IsEnabled = stepDoc.Enabled;
            ordered.Add(item);
            stepMap.Remove(stepDoc.StepType);
        }

        foreach (var remaining in stepMap.Values.OrderBy(s => s.Category).ThenBy(s => s.DisplayName))
            ordered.Add(remaining);

        AvailableSteps.Clear();
        foreach (var item in ordered)
            AvailableSteps.Add(item);
    }

    private static bool IsStepEnabledByDefault(string stepType) =>
        stepType is not ("Psd" or "Stft");

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var result = new string(chars);
        return string.IsNullOrWhiteSpace(result) ? "analysis-plan" : result;
    }
}
