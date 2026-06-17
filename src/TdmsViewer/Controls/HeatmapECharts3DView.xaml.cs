using System.IO;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class HeatmapECharts3DView : UserControl
{
    private Task? _initTask;
    private bool _pageReady;

    public HeatmapECharts3DView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await EnsureWebViewReadyAsync();
    }

    public async Task RenderAsync(HeatmapChartModel model, double colorMin, double colorMax)
    {
        if (model.XAxis.Length < 2 || model.YAxis.Length < 2)
            return;

        await EnsureWebViewReadyAsync();
        if (!_pageReady)
            return;

        var json = HeatmapEChartsPayloadBuilder.BuildJson(model, colorMin, colorMax);
        await WebView.ExecuteScriptAsync($"window.renderHeatmapSurface({json})");
    }

    private async Task EnsureWebViewReadyAsync()
    {
        if (_pageReady)
            return;

        if (_initTask != null)
        {
            await _initTask.ConfigureAwait(true);
            return;
        }

        _initTask = InitializeWebViewAsync();
        await _initTask.ConfigureAwait(true);
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TdmsViewer",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment
            .CreateAsync(null, userDataFolder)
            .ConfigureAwait(true);
        await WebView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);

        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Web", "heatmap-surface3d.html");
        var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            WebView.NavigationCompleted -= OnNavigationCompleted;
            navigation.TrySetResult(args.IsSuccess);
        }

        WebView.NavigationCompleted += OnNavigationCompleted;
        WebView.Source = new Uri(htmlPath);
        _pageReady = await navigation.Task.ConfigureAwait(true);
    }
}
