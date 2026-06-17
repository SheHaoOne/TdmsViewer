using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisReportViewModel : ObservableObject
{
    private AnalysisReportModel? _fullReport;
    private bool _suppressOverlaySelectAllSync;
    private readonly Dictionary<string, HeatmapChartViewModel> _heatmapViewModels = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private ReportMeta? _meta;

    [ObservableProperty]
    private string _summaryText = "暂无报表";

    [ObservableProperty]
    private bool _hasOverlayControls;

    [ObservableProperty]
    private bool _isAllOverlayChecked = true;

    public ObservableCollection<object> Cards { get; } = new();

    public ObservableCollection<ReportOverlayFileItem> OverlayFiles { get; } = new();

    partial void OnIsAllOverlayCheckedChanged(bool value)
    {
        if (_suppressOverlaySelectAllSync)
            return;

        foreach (var file in OverlayFiles)
            file.IsVisible = value;

        ApplyOverlayFilter();
    }

    public void SetReport(AnalysisReportModel report)
    {
        ClearOverlaySubscriptions();
        _heatmapViewModels.Clear();
        _fullReport = report;
        Meta = report.Meta;

        OverlayFiles.Clear();
        foreach (var source in report.Sources)
        {
            var item = new ReportOverlayFileItem
            {
                FilePath = source.FilePath,
                FileName = source.FileName,
                PlotColor = source.Color,
                IsVisible = true
            };
            item.PropertyChanged += OnOverlayItemPropertyChanged;
            OverlayFiles.Add(item);
        }

        HasOverlayControls = OverlayFiles.Count > 1;
        SyncSelectAllOverlayCheckbox();
        ApplyOverlayFilter();
    }

    [RelayCommand]
    private void Clear()
    {
        ClearOverlaySubscriptions();
        _heatmapViewModels.Clear();
        _fullReport = null;
        Meta = null;
        SummaryText = "暂无报表";
        HasOverlayControls = false;
        OverlayFiles.Clear();
        Cards.Clear();
    }

    private void OnOverlayItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ReportOverlayFileItem.IsVisible))
            return;

        SyncSelectAllOverlayCheckbox();
        ApplyOverlayFilter();
    }

    private void ApplyOverlayFilter()
    {
        if (_fullReport == null)
        {
            Cards.Clear();
            SummaryText = "暂无报表";
            return;
        }

        var visiblePaths = OverlayFiles
            .Where(f => f.IsVisible)
            .Select(f => f.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (visiblePaths.Count == 0)
        {
            Cards.Clear();
            SummaryText = BuildSummaryText(_fullReport.Meta, 0);
            return;
        }

        var filtered = AnalysisReportOverlayFilter.Filter(_fullReport.Cards, visiblePaths);
        Cards.Clear();
        foreach (var card in filtered)
            Cards.Add(ToDisplayCard(card));

        SummaryText = BuildSummaryText(_fullReport.Meta, visiblePaths.Count);
    }

    private object ToDisplayCard(ChartCardModel card) =>
        card switch
        {
            HeatmapChartModel heatmap => GetOrCreateHeatmapViewModel(heatmap),
            _ => card
        };

    private HeatmapChartViewModel GetOrCreateHeatmapViewModel(HeatmapChartModel heatmap)
    {
        if (!_heatmapViewModels.TryGetValue(heatmap.Id, out var viewModel))
        {
            viewModel = new HeatmapChartViewModel(heatmap);
            _heatmapViewModels[heatmap.Id] = viewModel;
        }

        return viewModel;
    }

    private void SyncSelectAllOverlayCheckbox()
    {
        _suppressOverlaySelectAllSync = true;
        IsAllOverlayChecked = OverlayFiles.Count > 0 && OverlayFiles.All(f => f.IsVisible);
        _suppressOverlaySelectAllSync = false;
    }

    private void ClearOverlaySubscriptions()
    {
        foreach (var item in OverlayFiles)
            item.PropertyChanged -= OnOverlayItemPropertyChanged;
    }

    private static string BuildSummaryText(ReportMeta meta, int visibleSourceCount)
    {
        var sourceLabel = visibleSourceCount <= 1
            ? meta.FileName
            : $"{meta.ChannelName}（{visibleSourceCount} 个文件叠加）";

        return $"{sourceLabel} · {meta.GroupName} / {meta.ChannelName} · " +
               $"{meta.SampleRateHz:N0} Hz · {meta.SampleCount:N0} 点 · " +
               (string.IsNullOrWhiteSpace(meta.TimeRangeSummary) ? string.Empty : $"{meta.TimeRangeSummary} · ") +
               $"{meta.GeneratedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
