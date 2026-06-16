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

        var count = Math.Min(Model.Categories.Length, Model.Values.Length);
        if (count == 0)
        {
            PlotHost.Refresh();
            return;
        }

        var bars = new Bar[count];
        for (var i = 0; i < count; i++)
        {
            bars[i] = new Bar
            {
                Position = i,
                Value = Model.Values[i],
                FillColor = Color.FromHex("#007AFF"),
                LineColor = Color.FromHex("#007AFF")
            };
        }

        PlotHost.Plot.Add.Bars(bars);

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
}
