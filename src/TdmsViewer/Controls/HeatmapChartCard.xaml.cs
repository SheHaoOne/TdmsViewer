using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class HeatmapChartCard : UserControl
{
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(HeatmapChartModel),
            typeof(HeatmapChartCard),
            new PropertyMetadata(null, OnModelChanged));

    public HeatmapChartCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public HeatmapChartModel? Model
    {
        get => (HeatmapChartModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HeatmapChartCard card)
            card.Redraw();
    }

    private void Redraw()
    {
        if (!IsLoaded || Model == null)
            return;

        PlotHost.Plot.Clear();
        ScottPlotStyle.ApplyMacTheme(PlotHost.Plot);

        if (Model.XAxis.Length < 2 || Model.YAxis.Length < 2)
        {
            PlotHost.Refresh();
            return;
        }

        var heatmap = PlotHost.Plot.Add.Heatmap(Model.Values);
        heatmap.Smooth = true;
        heatmap.FlipVertically = true;
        heatmap.Rectangle = new CoordinateRect(
            Model.XAxis[0],
            Model.XAxis[^1],
            Model.YAxis[0],
            Model.YAxis[^1]);
        heatmap.Colormap = new ScottPlot.Colormaps.Turbo();

        if (Model.ColorMin is double min && Model.ColorMax is double max)
            heatmap.ManualRange = new ScottPlot.Range(min, max);

        PlotHost.Plot.Add.ColorBar(heatmap);
        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }
}
