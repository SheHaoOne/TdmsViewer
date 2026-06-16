using System.Collections.ObjectModel;
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
    private readonly Func<double[]?> _getCurrentChannelData;
    private readonly Func<bool> _canAnalyze;
    private readonly Func<ChannelSourceRef, double[]> _readChannelData;
    private readonly Action<AnalysisReport> _onReportReady;
    private readonly AnalysisStepRegistry _registry = new();
    private readonly PipelineRunner _runner;

    public AnalysisWorkbenchViewModel(
        Func<ChannelSourceRef?> getActiveSource,
        Func<double[]?> getCurrentChannelData,
        Func<bool> canAnalyze,
        Func<ChannelSourceRef, double[]> readChannelData,
        Action<AnalysisReport> onReportReady)
    {
        _getActiveSource = getActiveSource;
        _getCurrentChannelData = getCurrentChannelData;
        _canAnalyze = canAnalyze;
        _readChannelData = readChannelData;
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
                IsEnabled = true
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

    public ObservableCollection<AnalysisStepItem> AvailableSteps { get; } = new();

    [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
    private async Task RunAnalysisAsync()
    {
        var source = _getActiveSource();
        if (source == null)
        {
            MessageBox.Show("请先在左侧选择通道，并单击文件名。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var enabledSteps = AvailableSteps.Where(s => s.IsEnabled).ToList();
        if (enabledSteps.Count == 0)
        {
            MessageBox.Show("请至少启用一个分析步骤。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sampleRate = source.Channel.SampleRateHz;
        if (sampleRate is not > 0)
        {
            MessageBox.Show(
                "当前通道缺少有效采样率（wf_increment 或 NI_SampleRate）。\nNVH 分析需要采样率大于 0。",
                "分析编排",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsRunning = true;
            StatusMessage = "正在准备数据…";

            var data = _getCurrentChannelData();
            if (data == null || data.Length == 0)
            {
                StatusMessage = "正在读取通道数据…";
                data = await Task.Run(() => _readChannelData(source)).ConfigureAwait(true);
            }

            if (data.Length == 0)
            {
                MessageBox.Show("当前通道没有可分析的数据。", "分析编排", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var input = new AnalysisInputContext
            {
                FilePath = source.FilePath,
                FileName = source.FileName,
                GroupName = source.Channel.GroupName,
                ChannelName = source.Channel.ChannelName,
                Samples = data,
                SampleRateHz = sampleRate.Value
            };

            var plan = BuildPlan();
            var progress = new Progress<PipelineProgress>(p =>
            {
                ProgressText = $"{p.CurrentStepName} ({p.CompletedSteps}/{p.TotalSteps})";
                StatusMessage = $"正在分析：{p.CurrentStepName}…";
            });

            var report = await Task.Run(() => _runner.RunAsync(plan, input, progress)).ConfigureAwait(true);
            _onReportReady(report);
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

    private bool CanRunAnalysis() => !IsRunning && _canAnalyze();

    partial void OnIsRunningChanged(bool value) => RunAnalysisCommand.NotifyCanExecuteChanged();

    public void NotifySessionChanged() => RunAnalysisCommand.NotifyCanExecuteChanged();

    private AnalysisPlan BuildPlan()
    {
        var defaultIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Spl"] = "spl",
            ["TimeDomain"] = "td",
            ["Waveform"] = "wf",
            ["Spectrum"] = "sp",
            ["OctaveBands"] = "ob"
        };

        return new AnalysisPlan
        {
            Name = string.IsNullOrWhiteSpace(PlanName) ? "NVH 声学分析" : PlanName.Trim(),
            DashboardTemplateId = "nvh-acoustic-light",
            Steps = AvailableSteps
                .Where(s => s.IsEnabled)
                .Select(s => new AnalysisPlanStep
                {
                    StepType = s.StepType,
                    Id = defaultIds.TryGetValue(s.StepType, out var id) ? id : s.StepType.ToLowerInvariant(),
                    Enabled = true
                })
                .ToList()
        };
    }
}
