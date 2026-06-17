using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.TickGenerators;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class LineChartCard : UserControl
{
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(LineChartModel),
            typeof(LineChartCard),
            new PropertyMetadata(null, OnModelChanged));

    public LineChartCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public LineChartModel? Model
    {
        get => (LineChartModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LineChartCard card)
            card.Redraw();
    }

    private void Redraw()
    {
        if (!IsLoaded || Model == null)
            return;

        PlotHost.Plot.Clear();
        ScottPlotStyle.ApplyMacTheme(PlotHost.Plot);

        foreach (var series in Model.Series)
        {
            if (series.X.Length < 1 || series.Y.Length < 1)
                continue;

            var count = Math.Min(series.X.Length, series.Y.Length);
            var xs = series.X.AsSpan(0, count).ToArray();
            var ys = series.Y.AsSpan(0, count).ToArray();

            if (Model.UseLogXAxis)
                xs = PlotDataHelper.ToLog10Axis(xs);

            if (Model.UseStepLine)
            {
                var scatter = PlotHost.Plot.Add.Scatter(xs, ys);
                scatter.Color = Color.FromHex(series.Color);
                scatter.LineWidth = 1.5f;
                scatter.ConnectStyle = ConnectStyle.StepHorizontal;
                scatter.MarkerSize = 0;
            }
            else if (xs.Length >= 2)
            {
                var sig = PlotHost.Plot.Add.SignalXY(xs, ys);
                sig.Color = Color.FromHex(series.Color);
                sig.LineWidth = 1.5f;
            }
        }

        if (Model.UseLogXAxis)
            ConfigureLogXAxisFromCenters(Model.Series.SelectMany(s => s.X));

        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.HideLegend();
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }

    private void ConfigureLogXAxisFromCenters(IEnumerable<double> centerFrequenciesHz)
    {
        var ticks = centerFrequenciesHz
            .Where(hz => hz > 0)
            .Distinct()
            .OrderBy(hz => hz)
            .Select(hz => new Tick(Math.Log10(hz), PlotDataHelper.FormatFrequencyLabel(hz)))
            .ToArray();

        if (ticks.Length == 0)
            return;

        PlotHost.Plot.Axes.Bottom.TickGenerator = new NumericManual(ticks);
        PlotHost.Plot.Grid.MinorLineColor = Color.FromHex("#D2D2D7").WithAlpha(0.25);
        PlotHost.Plot.Grid.MinorLineWidth = 1;
    }
}
