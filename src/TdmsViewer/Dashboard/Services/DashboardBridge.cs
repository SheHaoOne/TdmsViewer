using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Dashboard.Services;

public sealed class DashboardBridge : IAsyncDisposable
{
    private Microsoft.Web.WebView2.Wpf.WebView2? _webView;
    private bool _hostMapped;

    public async Task AttachAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
    {
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

        webView.Source = new Uri("https://tdmsviewer.assets/dashboard.html");
    }

    public async Task RenderReportAsync(AnalysisReport report)
    {
        if (_webView?.CoreWebView2 == null)
            return;

        var json = ReportSerializer.ToJson(report);
        var script = $"window.renderTdmsReport({json});";
        await _webView.CoreWebView2.ExecuteScriptAsync(script).ConfigureAwait(true);
    }

    public ValueTask DisposeAsync()
    {
        _webView = null;
        return ValueTask.CompletedTask;
    }
}
