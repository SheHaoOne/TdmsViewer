using System.Windows;
using System.Windows.Controls;
using ScottPlot;
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
            if (series.X.Length < 2 || series.Y.Length < 2)
                continue;

            var sig = PlotHost.Plot.Add.SignalXY(series.X, series.Y);
            sig.Color = Color.FromHex(series.Color);
            sig.LineWidth = 1.5f;
        }

        PlotHost.Plot.Axes.Bottom.Label.Text = Model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = Model.YLabel;
        PlotHost.Plot.HideLegend();
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
    }
}
