using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

    public static readonly DependencyProperty IsHostActiveProperty =
        DependencyProperty.Register(
            nameof(IsHostActive),
            typeof(bool),
            typeof(DashboardWebView),
            new PropertyMetadata(false, OnIsHostActiveChanged));

    private readonly DashboardBridge _bridge = new();
    private bool _initialized;

    public DashboardWebView()
    {
        InitializeComponent();
        WebView.Visibility = Visibility.Collapsed;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public AnalysisReport? Report
    {
        get => (AnalysisReport?)GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    public bool IsHostActive
    {
        get => (bool)GetValue(IsHostActiveProperty);
        set => SetValue(IsHostActiveProperty, value);
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsHostActive == IsVisible)
            return;

        IsHostActive = IsVisible;
    }

    private static void OnIsHostActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DashboardWebView control)
            return;

        if (e.NewValue is true)
            _ = control.EnsureInitializedAsync();
        else
            control.HideWebView();
    }

    private static async void OnReportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DashboardWebView control)
            return;

        if (e.NewValue is not AnalysisReport report)
            return;

        if (!control.IsHostActive)
            return;

        await control.EnsureInitializedAsync().ConfigureAwait(true);

        if (!control._initialized)
            return;

        try
        {
            await control._bridge.RenderReportAsync(report).ConfigureAwait(true);
            control.PlaceholderText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            control.PlaceholderText.Text = $"报表渲染失败：{ex.Message}";
            control.PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized || !IsHostActive)
            return;

        try
        {
            PlaceholderText.Text = "正在加载分析大屏…";
            PlaceholderText.Visibility = Visibility.Visible;
            WebView.Visibility = Visibility.Visible;

            await _bridge.AttachAsync(WebView).ConfigureAwait(true);
            _initialized = true;

            if (Report != null)
                await _bridge.RenderReportAsync(Report).ConfigureAwait(true);

            PlaceholderText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            WebView.Visibility = Visibility.Collapsed;
            PlaceholderText.Text = $"大屏加载失败：{ex.Message}";
            PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private void HideWebView()
    {
        WebView.Visibility = Visibility.Collapsed;
        PlaceholderText.Visibility = Visibility.Collapsed;
    }
}
