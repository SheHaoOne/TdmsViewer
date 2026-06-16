using System.IO;
using Microsoft.Web.WebView2.Core;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Dashboard.Services;

public sealed class DashboardBridge
{
    private const string DashboardUrl = "https://tdmsviewer.assets/dashboard.html";

    private Microsoft.Web.WebView2.Wpf.WebView2? _webView;
    private TaskCompletionSource<bool>? _navigationTcs;
    private bool _hostMapped;
    private bool _pageReady;
    private AnalysisReport? _pendingReport;

    public async Task AttachAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
    {
        if (_webView != null && !ReferenceEquals(_webView, webView))
        {
            ResetNavigationState();
        }

        _webView = webView;
        await webView.EnsureCoreWebView2Async().ConfigureAwait(true);

        var assetsFolder = Path.Combine(AppContext.BaseDirectory, "Dashboard", "Assets");
        if (!Directory.Exists(assetsFolder))
            throw new DirectoryNotFoundException($"未找到大屏资源目录：{assetsFolder}");

        if (!_hostMapped)
        {
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "tdmsviewer.assets",
                assetsFolder,
                CoreWebView2HostResourceAccessKind.Allow);
            _hostMapped = true;
        }

        webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

        var currentUrl = webView.Source?.ToString();
        if (_pageReady && string.Equals(currentUrl, DashboardUrl, StringComparison.OrdinalIgnoreCase))
        {
            if (_pendingReport != null)
                await RenderReportCoreAsync(_pendingReport).ConfigureAwait(true);
            return;
        }

        _pageReady = false;
        _navigationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        webView.Source = new Uri(DashboardUrl);
        await WaitForPageReadyAsync().ConfigureAwait(true);
    }

    public async Task RenderReportAsync(AnalysisReport report)
    {
        _pendingReport = report;
        await WaitForPageReadyAsync().ConfigureAwait(true);
        await RenderReportCoreAsync(report).ConfigureAwait(true);
    }

    public void Detach()
    {
        if (_webView?.CoreWebView2 != null)
            _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;

        _webView = null;
        ResetNavigationState();
    }

    private async Task RenderReportCoreAsync(AnalysisReport report)
    {
        if (_webView?.CoreWebView2 == null)
            return;

        var json = ReportSerializer.ToJson(report);
        _webView.CoreWebView2.PostWebMessageAsJson(json);

        // Fallback for hosts where PostWebMessage is delayed.
        var script = $"window.renderTdmsReport && window.renderTdmsReport({json});";
        await _webView.CoreWebView2.ExecuteScriptAsync(script).ConfigureAwait(true);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _navigationTcs?.TrySetException(new InvalidOperationException($"大屏页面加载失败：{e.WebErrorStatus}"));
            return;
        }

        if (!string.Equals(_webView?.Source?.ToString(), DashboardUrl, StringComparison.OrdinalIgnoreCase))
            return;

        _pageReady = true;
        _navigationTcs?.TrySetResult(true);

        if (_pendingReport != null && _webView?.CoreWebView2 != null)
        {
            _ = RenderReportCoreAsync(_pendingReport);
        }
    }

    private async Task WaitForPageReadyAsync()
    {
        if (_pageReady)
            return;

        if (_navigationTcs == null)
            return;

        await _navigationTcs.Task.ConfigureAwait(true);
    }

    private void ResetNavigationState()
    {
        _navigationTcs = null;
        _pageReady = false;
        _pendingReport = null;
    }
}
