using System.Windows;
using System.Windows.Controls;

namespace TdmsViewer.Controls;

internal static class ChartFullscreenService
{
    public static void Show(UIElement source, Func<UIElement> contentFactory, string title)
    {
        var owner = Window.GetWindow(source);
        var content = contentFactory();
        var window = new ChartFullscreenWindow(content, title)
        {
            Owner = owner ?? Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    public static void ApplyFullscreenLayout(UIElement content)
    {
        switch (content)
        {
            case LineChartCard lineChart:
                lineChart.ApplyFullscreenLayout();
                break;
            case BarChartCard barChart:
                barChart.ApplyFullscreenLayout();
                break;
            case HeatmapChartCard heatmapChart:
                heatmapChart.ApplyFullscreenLayout();
                break;
            case MetricCard metricCard:
                metricCard.ApplyFullscreenLayout();
                break;
        }
    }

    public static void RefreshChartContent(UIElement content)
    {
        switch (content)
        {
            case LineChartCard lineChart:
                lineChart.RefreshAfterResize();
                break;
            case BarChartCard barChart:
                barChart.RefreshAfterResize();
                break;
            case HeatmapChartCard heatmapChart:
                heatmapChart.RefreshAfterResize();
                break;
        }
    }
}
