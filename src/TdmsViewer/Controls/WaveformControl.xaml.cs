using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.Plottables;
using TdmsViewer.Models;

namespace TdmsViewer.Controls;

public partial class WaveformControl : UserControl
{
    private static readonly Color GridColor = Color.FromHex("#D2D2D7");
    private static readonly Color PlotBgColor = Color.FromHex("#E8E8ED");

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series),
            typeof(IEnumerable<WaveformSeries>),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnSeriesChanged));

    public WaveformControl()
    {
        InitializeComponent();
        ConfigurePlotStyle();
        Loaded += (_, _) => Redraw();
    }

    public IEnumerable<WaveformSeries>? Series
    {
        get => (IEnumerable<WaveformSeries>?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WaveformControl control)
            return;

        if (e.OldValue is INotifyCollectionChanged oldNotify)
            oldNotify.CollectionChanged -= control.OnCollectionChanged;

        if (e.NewValue is INotifyCollectionChanged newNotify)
            newNotify.CollectionChanged += control.OnCollectionChanged;

        control.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.Invoke(Redraw);

    private void ConfigurePlotStyle()
    {
        WpfPlot.Plot.FigureBackground.Color = Colors.Transparent;
        WpfPlot.Plot.DataBackground.Color = PlotBgColor;
        WpfPlot.Plot.Grid.MajorLineColor = GridColor.WithAlpha(0.6);
        WpfPlot.Plot.Grid.MinorLineColor = Colors.Transparent;
        WpfPlot.Plot.Axes.Color(GridColor);
        WpfPlot.Plot.HideLegend();
    }

    private void Redraw()
    {
        WpfPlot.Plot.Clear();
        ConfigurePlotStyle();

        var seriesList = Series?.Where(s => s.Points.Count >= 2).ToList() ?? new List<WaveformSeries>();
        EmptyHint.Visibility = seriesList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (seriesList.Count == 0)
        {
            WpfPlot.Refresh();
            return;
        }

        foreach (var series in seriesList)
        {
            var xs = series.Points.Select(p => p.X).ToArray();
            var ys = series.Points.Select(p => p.Y).ToArray();

            SignalXY sig = WpfPlot.Plot.Add.SignalXY(xs, ys);
            sig.Color = Color.FromHex(series.Color);
            sig.LineWidth = 1.5f;
        }

        WpfPlot.Plot.Axes.AutoScale();
        WpfPlot.Refresh();
    }
}
