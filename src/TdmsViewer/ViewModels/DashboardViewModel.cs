using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private AnalysisReport? _currentReport;

    [ObservableProperty]
    private string _statusMessage = "暂无分析报表，请先在「分析编排」中运行分析。";

    public void SetReport(AnalysisReport report)
    {
        CurrentReport = report;
        StatusMessage = $"{report.Meta.FileName} — {report.Meta.ChannelName} · {report.Blocks.Count} 项";
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

    private bool HasReport() => CurrentReport != null;

    partial void OnCurrentReportChanged(AnalysisReport? value) =>
        ExportHtmlCommand.NotifyCanExecuteChanged();
}
