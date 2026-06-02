using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.Plottables;
using TdmsViewer.Models;

namespace TdmsViewer.Controls;

public partial class WaveformControl : UserControl
{
    private static readonly Color AccentColor = Color.FromHex("#007AFF");
    private static readonly Color GridColor = Color.FromHex("#D2D2D7");
    private static readonly Color PlotBgColor = Color.FromHex("#E8E8ED");

    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(
            nameof(Points),
            typeof(IEnumerable<WaveformPoint>),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnPointsChanged));

    public WaveformControl()
    {
        InitializeComponent();
        ConfigurePlotStyle();
        Loaded += (_, _) => Redraw();
    }

    public IEnumerable<WaveformPoint>? Points
    {
        get => (IEnumerable<WaveformPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
    }

    private void Redraw()
    {
        WpfPlot.Plot.Clear();
        ConfigurePlotStyle();

        var points = Points?.ToList() ?? new List<WaveformPoint>();
        EmptyHint.Visibility = points.Count < 2 ? Visibility.Visible : Visibility.Collapsed;

        if (points.Count < 2)
        {
            WpfPlot.Refresh();
            return;
        }

        var xs = points.Select(p => p.X).ToArray();
        var ys = points.Select(p => p.Y).ToArray();

        SignalXY sig = WpfPlot.Plot.Add.SignalXY(xs, ys);
        sig.Color = AccentColor;
        sig.LineWidth = 1.5f;

        WpfPlot.Plot.Axes.AutoScale();
        WpfPlot.Refresh();
    }
}
