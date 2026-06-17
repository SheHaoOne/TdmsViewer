using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

public partial class BarChartCard : UserControl
{
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(BarChartModel),
            typeof(BarChartCard),
            new PropertyMetadata(null, OnModelChanged));

    public BarChartCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public BarChartModel? Model
    {
        get => (BarChartModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BarChartCard card)
            card.Redraw();
    }

    private void Redraw()
    {
        if (!IsLoaded || Model == null)
            return;

        PlotHost.Plot.Clear();
        ScottPlotStyle.ApplyMacTheme(PlotHost.Plot);

        var count = Model.Categories.Length;
        if (count == 0)
        {
            PlotHost.Refresh();
            return;
        }

        var overlaySeries = Model.Series is { Count: > 1 } ? Model.Series : null;
        if (overlaySeries != null)
            DrawGroupedBars(overlaySeries, count);
        else
            DrawSingleSeries(Model.Values, count, "#007AFF");

        if (Model.CategoryLabels is { Length: > 0 })
        {
            var ticks = new ScottPlot.Tick[count];
            for (var i = 0; i < count; i++)
            {
                var label = i < Model.CategoryLabels.Length ? Model.CategoryLabels[i] : i.ToString();
                ticks[i] = new ScottPlot.Tick(i, label);
            }

            PlotHost.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        }

        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }

    private void DrawSingleSeries(double[] values, int count, string color)
    {
        var seriesCount = Math.Min(count, values.Length);
        var bars = new Bar[seriesCount];
        for (var i = 0; i < seriesCount; i++)
        {
            bars[i] = new Bar
            {
                Position = i,
                Value = values[i],
                FillColor = Color.FromHex(color),
                LineColor = Color.FromHex(color)
            };
        }

        PlotHost.Plot.Add.Bars(bars);
    }

    private void DrawGroupedBars(IReadOnlyList<BarSeriesData> seriesList, int count)
    {
        const double groupWidth = 0.8;
        var barWidth = groupWidth / seriesList.Count;

        for (var seriesIndex = 0; seriesIndex < seriesList.Count; seriesIndex++)
        {
            var series = seriesList[seriesIndex];
            var seriesCount = Math.Min(count, series.Values.Length);
            var offset = (seriesIndex - (seriesList.Count - 1) / 2.0) * barWidth;
            var bars = new Bar[seriesCount];

            for (var i = 0; i < seriesCount; i++)
            {
                bars[i] = new Bar
                {
                    Position = i + offset,
                    Value = series.Values[i],
                    FillColor = Color.FromHex(series.Color),
                    LineColor = Color.FromHex(series.Color)
                };
            }

            PlotHost.Plot.Add.Bars(bars);
        }
    }

    internal void ApplyFullscreenLayout()
    {
        MinHeight = 0;
        ChartRow.Height = new GridLength(1, GridUnitType.Star);
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        if (Model == null)
            return;

        ChartFullscreenService.Show(
            this,
            () => new BarChartCard { Model = Model },
            Model.Title);
    }
}
