using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.ViewModels;

public sealed partial class ReportHistoryItem : ObservableObject
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required AnalysisReport Report { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private AnalysisReport? _currentReport;

    [ObservableProperty]
    private ReportHistoryItem? _selectedHistoryItem;

    [ObservableProperty]
    private string _statusMessage = "暂无分析报表，请先在「分析编排」中运行分析。";

    public ObservableCollection<ReportHistoryItem> ReportHistory { get; } = new();

    public void ApplyResult(AnalysisWorkbenchResult result)
    {
        foreach (var report in result.AllReports)
        {
            var item = new ReportHistoryItem
            {
                Id = Guid.NewGuid().ToString("N"),
                DisplayName = $"{report.Meta.FileName} · {report.Meta.ChannelName}",
                Report = report,
                CreatedAt = report.Meta.GeneratedAt
            };
            ReportHistory.Insert(0, item);
        }

        CurrentReport = result.PrimaryReport;
        SelectedHistoryItem = ReportHistory.FirstOrDefault(h =>
            ReferenceEquals(h.Report, result.PrimaryReport)) ?? ReportHistory.FirstOrDefault();

        StatusMessage = result.AllReports.Count > 1
            ? $"批量对比 — {result.AllReports.Count} 个文件"
            : $"{result.PrimaryReport.Meta.FileName} — {result.PrimaryReport.Meta.ChannelName} · {result.PrimaryReport.Blocks.Count} 项";
    }

    public void ClearHistory()
    {
        ReportHistory.Clear();
        CurrentReport = null;
        SelectedHistoryItem = null;
        StatusMessage = "暂无分析报表，请先在「分析编排」中运行分析。";
        ExportHtmlCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedHistoryItemChanged(ReportHistoryItem? value)
    {
        if (value != null)
            CurrentReport = value.Report;
    }

    [RelayCommand(CanExecute = nameof(HasReport))]
    private async Task ExportHtmlAsync()
    {
        if (CurrentReport == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "HTML 报表 (*.html)|*.html",
            FileName = $"{CurrentReport.Meta.ChannelName}_analysis.html",
            Title = "导出分析报表"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await ReportSerializer.ExportHtmlAsync(CurrentReport, dialog.FileName);
            StatusMessage = $"已导出 {dialog.FileName}";
            MessageBox.Show("报表导出成功。", "导出", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(HasHistory))]
    private void ClearAllReports() => ClearHistory();

    private bool HasReport() => CurrentReport != null;

    private bool HasHistory() => ReportHistory.Count > 0;

    partial void OnCurrentReportChanged(AnalysisReport? value)
    {
        ExportHtmlCommand.NotifyCanExecuteChanged();
        ClearAllReportsCommand.NotifyCanExecuteChanged();
    }
}
