using System.Windows;
using System.Windows.Controls;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Dashboard.Services;

namespace TdmsViewer.Dashboard.Controls;

public partial class DashboardWebView : UserControl
{
    public static readonly DependencyProperty ReportProperty =
        DependencyProperty.Register(
            nameof(Report),
            typeof(AnalysisReport),
            typeof(DashboardWebView),
            new PropertyMetadata(null, OnReportChanged));

    private readonly DashboardBridge _bridge = new();
    private bool _initialized;

    public DashboardWebView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public AnalysisReport? Report
    {
        get => (AnalysisReport?)GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        try
        {
            await _bridge.AttachAsync(WebView);
            _initialized = true;

            if (Report != null)
                await _bridge.RenderReportAsync(Report);
        }
        catch (Exception ex)
        {
            PlaceholderText.Text = $"大屏加载失败：{ex.Message}";
            PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private static async void OnReportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DashboardWebView control || e.NewValue is not AnalysisReport report)
            return;

        if (!control._initialized)
            return;

        try
        {
            await control._bridge.RenderReportAsync(report);
            control.PlaceholderText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            control.PlaceholderText.Text = $"报表渲染失败：{ex.Message}";
            control.PlaceholderText.Visibility = Visibility.Visible;
        }
    }
}
