using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScottPlot.WPF;
using TdmsViewer.Controls;

namespace TdmsViewer.Views;

public partial class AnalysisReportView : UserControl
{
    public AnalysisReportView() => InitializeComponent();

    private void ReportScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsOverChart(e.OriginalSource as DependencyObject))
            return;

        // Neutralize ScrollViewer scrolling while still letting ScottPlot receive MouseWheel for zoom.
        var scrollViewer = (ScrollViewer)sender;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
    }

    private static bool IsOverChart(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is WpfPlot or LineChartCard or BarChartCard)
                return true;

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
