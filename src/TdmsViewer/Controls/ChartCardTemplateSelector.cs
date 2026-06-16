using System.Windows;
using System.Windows.Controls;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public sealed class ChartCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LineChartTemplate { get; set; }
    public DataTemplate? BarChartTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            LineChartModel => LineChartTemplate,
            BarChartModel => BarChartTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
