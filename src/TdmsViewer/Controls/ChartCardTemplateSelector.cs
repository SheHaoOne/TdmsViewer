using System.Windows;
using System.Windows.Controls;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Controls;

public sealed class ChartCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LineChartTemplate { get; set; }
    public DataTemplate? BarChartTemplate { get; set; }
    public DataTemplate? HeatmapChartTemplate { get; set; }
    public DataTemplate? MetricCardTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            LineChartModel => LineChartTemplate,
            BarChartModel => BarChartTemplate,
            HeatmapChartModel => HeatmapChartTemplate,
            HeatmapChartViewModel => HeatmapChartTemplate,
            MetricCardModel => MetricCardTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
