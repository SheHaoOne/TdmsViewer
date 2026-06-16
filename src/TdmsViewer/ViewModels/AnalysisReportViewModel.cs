using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisReportViewModel : ObservableObject
{
    [ObservableProperty]
    private ReportMeta? _meta;

    [ObservableProperty]
    private string _summaryText = "暂无报表";

    public ObservableCollection<ChartCardModel> Cards { get; } = new();

    public void SetReport(AnalysisReportModel report)
    {
        Meta = report.Meta;
        SummaryText = $"{report.Meta.FileName} · {report.Meta.GroupName} / {report.Meta.ChannelName} · " +
                      $"{report.Meta.SampleRateHz:N0} Hz · {report.Meta.SampleCount:N0} 点 · " +
                      $"{report.Meta.GeneratedAt:yyyy-MM-dd HH:mm:ss}";

        Cards.Clear();
        foreach (var card in report.Cards)
            Cards.Add(card);
    }

    [RelayCommand]
    private void Clear()
    {
        Meta = null;
        SummaryText = "暂无报表";
        Cards.Clear();
    }
}
