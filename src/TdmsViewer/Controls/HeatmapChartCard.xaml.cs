using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.TickGenerators;
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
        if (Model.UseLogYAxis)
        {
            var yMin = Model.YAxis[0];
            var yMax = Model.YAxis[^1];
            heatmap.Rectangle = new CoordinateRect(
                Model.XAxis[0],
                Model.XAxis[^1],
                Math.Log10(yMin),
                Math.Log10(yMax));
            ConfigureLogYAxisFromCenters(Model.YAxis);
        }
        else
        {
            heatmap.Rectangle = new CoordinateRect(
                Model.XAxis[0],
                Model.XAxis[^1],
                Model.YAxis[0],
                Model.YAxis[^1]);
        }
        heatmap.Colormap = new ScottPlot.Colormaps.Turbo();

        if (Model.ColorMin is double min && Model.ColorMax is double max)
            heatmap.ManualRange = new ScottPlot.Range(min, max);

        PlotHost.Plot.Add.ColorBar(heatmap);
        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }

    private void ConfigureLogYAxisFromCenters(IReadOnlyList<double> centerFrequenciesHz)
    {
        var ticks = centerFrequenciesHz
            .Where(hz => hz > 0)
            .Distinct()
            .OrderBy(hz => hz)
            .Select(hz => new Tick(Math.Log10(hz), PlotDataHelper.FormatFrequencyLabel(hz)))
            .ToArray();

        if (ticks.Length == 0)
            return;

        PlotHost.Plot.Axes.Left.TickGenerator = new NumericManual(ticks);
        PlotHost.Plot.Grid.MinorLineColor = Color.FromHex("#D2D2D7").WithAlpha(0.25);
        PlotHost.Plot.Grid.MinorLineWidth = 1;
    }
}
