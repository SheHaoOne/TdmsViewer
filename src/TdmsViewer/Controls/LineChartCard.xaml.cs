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
            }
            else if (xs.Length >= 2)
            {
                var sig = PlotHost.Plot.Add.SignalXY(xs, ys);
                sig.Color = Color.FromHex(series.Color);
                sig.LineWidth = 1.5f;
            }
        }

        if (Model.UseLogXAxis)
            ConfigureLogXAxis();

        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.HideLegend();
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }

    private void ConfigureLogXAxis()
    {
        var tickGen = new NumericAutomatic
        {
            MinorTickGenerator = new LogDecadeMinorTickGenerator(),
            IntegerTicksOnly = true,
            LabelFormatter = static logValue => PlotDataHelper.FormatFrequencyLabel(Math.Pow(10, logValue))
        };

        PlotHost.Plot.Axes.Bottom.TickGenerator = tickGen;
        PlotHost.Plot.Grid.MinorLineColor = Color.FromHex("#D2D2D7").WithAlpha(0.25);
        PlotHost.Plot.Grid.MinorLineWidth = 1;
    }
}
