using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TdmsViewer.Models;

namespace TdmsViewer.Controls;

public partial class WaveformControl : UserControl
{
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(
            nameof(Points),
            typeof(IEnumerable<WaveformPoint>),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnPointsChanged));

    private Polyline? _polyline;

    public WaveformControl()
    {
        InitializeComponent();
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

    private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        PlotCanvas.Children.Clear();
        var points = Points?.ToList() ?? new List<WaveformPoint>();

        EmptyHint.Visibility = points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (points.Count < 2 || PlotCanvas.ActualWidth < 1 || PlotCanvas.ActualHeight < 1)
            return;

        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);
        var rangeX = Math.Max(maxX - minX, 1e-9);
        var rangeY = Math.Max(maxY - minY, 1e-9);

        var pad = 8.0;
        var w = PlotCanvas.ActualWidth - pad * 2;
        var h = PlotCanvas.ActualHeight - pad * 2;

        var polyPoints = new PointCollection(points.Count);
        foreach (var p in points)
        {
            var x = pad + (p.X - minX) / rangeX * w;
            var y = pad + h - (p.Y - minY) / rangeY * h;
            polyPoints.Add(new Point(x, y));
        }

        _polyline = new Polyline
        {
            Points = polyPoints,
            Stroke = (Brush)FindResource("MacAccentBrush"),
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        PlotCanvas.Children.Add(_polyline);

        // 零线
        if (minY < 0 && maxY > 0)
        {
            var zeroY = pad + h - (0 - minY) / rangeY * h;
            PlotCanvas.Children.Add(new Line
            {
                X1 = pad,
                X2 = pad + w,
                Y1 = zeroY,
                Y2 = zeroY,
                Stroke = (Brush)FindResource("MacSeparatorBrush"),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            });
        }
    }
}
