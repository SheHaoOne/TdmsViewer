using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScottPlot;
using ScottPlot.Plottables;
using TdmsViewer.Models;

namespace TdmsViewer.Controls;

public partial class WaveformControl : UserControl
{
    private const float MinSelectionPixels = 5f;
    private static readonly Color SelectionFillColor = Color.FromHex("#007AFF").WithAlpha(0.22);
    private static readonly Color SelectionLineColor = Color.FromHex("#007AFF").WithAlpha(0.85);
    private static readonly Color HighlightFillColor = Color.FromHex("#FF9500").WithAlpha(0.18);
    private static readonly Color HighlightLineColor = Color.FromHex("#FF9500").WithAlpha(0.75);

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series),
            typeof(IEnumerable<WaveformSeries>),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnSeriesChanged));

    public static readonly DependencyProperty HighlightStartSecProperty =
        DependencyProperty.Register(
            nameof(HighlightStartSec),
            typeof(double?),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnHighlightChanged));

    public static readonly DependencyProperty HighlightEndSecProperty =
        DependencyProperty.Register(
            nameof(HighlightEndSec),
            typeof(double?),
            typeof(WaveformControl),
            new PropertyMetadata(null, OnHighlightChanged));

    private HorizontalSpan? _selectionSpan;
    private HorizontalSpan? _highlightSpan;
    private bool _isSelecting;
    private double _selectionAnchorX;
    private Pixel _selectionAnchorPixel;
    private double _dataMinTimeSec;
    private double _dataMaxTimeSec;

    public event EventHandler<WaveformTimeRangeSelectedEventArgs>? TimeRangeSelected;

    public WaveformControl()
    {
        InitializeComponent();
        ConfigurePlotStyle();
        ConfigureSelectionInput();
        Loaded += (_, _) => Redraw();
    }

    public IEnumerable<WaveformSeries>? Series
    {
        get => (IEnumerable<WaveformSeries>?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public double? HighlightStartSec
    {
        get => (double?)GetValue(HighlightStartSecProperty);
        set => SetValue(HighlightStartSecProperty, value);
    }

    public double? HighlightEndSec
    {
        get => (double?)GetValue(HighlightEndSecProperty);
        set => SetValue(HighlightEndSecProperty, value);
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

    private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl control)
            control.UpdateHighlightOverlay();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.Invoke(Redraw);

    private void ConfigurePlotStyle()
    {
        ScottPlotStyle.ApplyMacTheme(WpfPlot.Plot);
        WpfPlot.Plot.HideLegend();
        WpfPlot.Plot.Axes.Bottom.Label.Text = "时间 (s)";
    }

    private void ConfigureSelectionInput()
    {
        WpfPlot.UserInputProcessor.IsEnabled = true;

        WpfPlot.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        WpfPlot.PreviewMouseMove += OnPreviewMouseMove;
        WpfPlot.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_dataMaxTimeSec <= _dataMinTimeSec)
            return;

        _isSelecting = true;
        _selectionAnchorPixel = WpfPlot.GetPlotPixelPosition(e);
        _selectionAnchorX = WpfPlot.Plot.GetCoordinates(_selectionAnchorPixel).X;
        WpfPlot.CaptureMouse();
        WpfPlot.UserInputProcessor.IsEnabled = false;

        _selectionSpan ??= CreateSpan(SelectionFillColor, SelectionLineColor);
        _selectionSpan.X1 = _selectionAnchorX;
        _selectionSpan.X2 = _selectionAnchorX;
        _selectionSpan.IsVisible = true;
        WpfPlot.Refresh();
        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting || _selectionSpan == null)
            return;

        var currentX = WpfPlot.Plot.GetCoordinates(WpfPlot.GetPlotPixelPosition(e)).X;
        _selectionSpan.X1 = Math.Min(_selectionAnchorX, currentX);
        _selectionSpan.X2 = Math.Max(_selectionAnchorX, currentX);
        WpfPlot.Refresh();
        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
            return;

        _isSelecting = false;
        WpfPlot.ReleaseMouseCapture();
        WpfPlot.UserInputProcessor.IsEnabled = true;

        var endPixel = WpfPlot.GetPlotPixelPosition(e);
        var dragDistance = Math.Abs(endPixel.X - _selectionAnchorPixel.X);
        if (dragDistance < MinSelectionPixels)
        {
            if (_selectionSpan != null)
                _selectionSpan.IsVisible = false;

            WpfPlot.Refresh();
            e.Handled = true;
            return;
        }

        if (_selectionSpan == null)
        {
            e.Handled = true;
            return;
        }

        var startSec = Math.Min(_selectionSpan.X1, _selectionSpan.X2);
        var endSec = Math.Max(_selectionSpan.X1, _selectionSpan.X2);
        startSec = Math.Clamp(startSec, _dataMinTimeSec, _dataMaxTimeSec);
        endSec = Math.Clamp(endSec, _dataMinTimeSec, _dataMaxTimeSec);

        if (endSec > startSec)
            TimeRangeSelected?.Invoke(this, new WaveformTimeRangeSelectedEventArgs(startSec, endSec));

        _selectionSpan.IsVisible = false;
        WpfPlot.Refresh();
        e.Handled = true;
    }

    private void Redraw()
    {
        WpfPlot.Plot.Clear();
        ConfigurePlotStyle();
        _selectionSpan = null;
        _highlightSpan = null;

        var seriesList = Series?.Where(s => s.Points.Count >= 2).ToList() ?? new List<WaveformSeries>();
        EmptyHint.Visibility = seriesList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (seriesList.Count == 0)
        {
            _dataMinTimeSec = 0;
            _dataMaxTimeSec = 0;
            WpfPlot.Refresh();
            return;
        }

        _dataMinTimeSec = double.PositiveInfinity;
        _dataMaxTimeSec = double.NegativeInfinity;

        foreach (var series in seriesList)
        {
            var xs = series.Points.Select(p => p.X).ToArray();
            var ys = series.Points.Select(p => p.Y).ToArray();
            _dataMinTimeSec = Math.Min(_dataMinTimeSec, xs[0]);
            _dataMaxTimeSec = Math.Max(_dataMaxTimeSec, xs[^1]);

            SignalXY sig = WpfPlot.Plot.Add.SignalXY(xs, ys);
            sig.Color = Color.FromHex(series.Color);
            sig.LineWidth = 1.5f;
        }

        WpfPlot.Plot.Axes.AutoScale();
        UpdateHighlightOverlay();
        WpfPlot.Refresh();
    }

    private void UpdateHighlightOverlay()
    {
        if (_dataMaxTimeSec <= _dataMinTimeSec)
            return;

        if (HighlightStartSec is not double startSec ||
            HighlightEndSec is not double endSec ||
            endSec <= startSec)
        {
            if (_highlightSpan != null)
                _highlightSpan.IsVisible = false;

            WpfPlot.Refresh();
            return;
        }

        startSec = Math.Clamp(startSec, _dataMinTimeSec, _dataMaxTimeSec);
        endSec = Math.Clamp(endSec, startSec, _dataMaxTimeSec);
        if (endSec <= startSec)
        {
            if (_highlightSpan != null)
                _highlightSpan.IsVisible = false;

            WpfPlot.Refresh();
            return;
        }

        _highlightSpan ??= CreateSpan(HighlightFillColor, HighlightLineColor);
        _highlightSpan.X1 = startSec;
        _highlightSpan.X2 = endSec;
        _highlightSpan.IsVisible = true;
        WpfPlot.Refresh();
    }

    private HorizontalSpan CreateSpan(Color fillColor, Color lineColor)
    {
        var span = WpfPlot.Plot.Add.HorizontalSpan(0, 0);
        span.FillColor = fillColor;
        span.LineColor = lineColor;
        span.LineWidth = 1f;
        span.IsDraggable = false;
        span.IsResizable = false;
        span.EnableAutoscale = false;
        span.IsVisible = false;
        return span;
    }
}
