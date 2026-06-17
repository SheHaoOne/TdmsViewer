using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScottPlot;
using ScottPlot.Panels;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Controls;

public partial class HeatmapChartCard : UserControl
{
    private const float ColorBarHandleFraction = 0.14f;
    private const double MinColorRangeSpanRatio = 0.01;

    private Heatmap? _heatmap;
    private bool _isColorBarDragging;
    private ColorBarDragTarget _dragTarget;
    private double _dragMin;
    private double _dragMax;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(HeatmapChartViewModel),
            typeof(HeatmapChartCard),
            new PropertyMetadata(null, OnViewModelChanged));

    public HeatmapChartCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public HeatmapChartViewModel? ViewModel
    {
        get => (HeatmapChartViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureColorBarInteraction();
        Redraw();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HeatmapChartCard card)
            return;

        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= card.OnViewModelPropertyChanged;

        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += card.OnViewModelPropertyChanged;

        card.Redraw();
    }

    private void ConfigureColorBarInteraction()
    {
        PlotHost.PreviewMouseLeftButtonDown += OnPlotMouseLeftButtonDown;
        PlotHost.PreviewMouseMove += OnPlotMouseMove;
        PlotHost.PreviewMouseLeftButtonUp += OnPlotMouseLeftButtonUp;
        PlotHost.MouseLeave += (_, _) => PlotHost.Cursor = Cursors.Arrow;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isColorBarDragging)
            return;

        if (e.PropertyName is nameof(HeatmapChartViewModel.RenderModel)
            or nameof(HeatmapChartViewModel.UseAutoColorRange)
            or nameof(HeatmapChartViewModel.ColorMinText)
            or nameof(HeatmapChartViewModel.ColorMaxText))
        {
            Redraw();
        }
    }

    private void OnPlotMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null || _heatmap == null)
            return;

        var pixel = PlotHost.GetPlotPixelPosition(e);
        var rect = TryGetColorBarBitmapRect();
        if (rect is not PixelRect colorBarRect || !colorBarRect.Contains(pixel))
            return;

        if (!ViewModel.TryGetColorRange(out _dragMin, out _dragMax))
            return;

        _dragTarget = HitTestDragTarget(colorBarRect, pixel);
        if (_dragTarget == ColorBarDragTarget.None)
            return;

        _isColorBarDragging = true;
        PlotHost.CaptureMouse();
        PlotHost.Cursor = Cursors.SizeNS;
        e.Handled = true;
    }

    private void OnPlotMouseMove(object sender, MouseEventArgs e)
    {
        var pixel = PlotHost.GetPlotPixelPosition(e);
        var rect = TryGetColorBarBitmapRect();

        if (!_isColorBarDragging)
        {
            UpdateHoverCursor(rect, pixel);
            return;
        }

        if (ViewModel == null || _heatmap == null || rect is not PixelRect colorBarRect)
            return;

        var minSpan = GetMinimumColorSpan(ViewModel);
        var candidate = PixelYToColorValue(colorBarRect, pixel.Y, _dragMin, _dragMax);

        if (_dragTarget == ColorBarDragTarget.Min)
            _dragMin = Math.Min(candidate, _dragMax - minSpan);
        else
            _dragMax = Math.Max(candidate, _dragMin + minSpan);

        ApplyColorRangeToHeatmap(_dragMin, _dragMax);
        ViewModel.ApplyDraggedColorRange(_dragMin, _dragMax);
        e.Handled = true;
    }

    private void OnPlotMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isColorBarDragging)
            return;

        _isColorBarDragging = false;
        _dragTarget = ColorBarDragTarget.None;
        PlotHost.ReleaseMouseCapture();
        ViewModel?.CommitManualColorRange(_dragMin, _dragMax);
        PlotHost.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void UpdateHoverCursor(PixelRect? rect, Pixel pixel)
    {
        if (rect is not PixelRect colorBarRect || !colorBarRect.Contains(pixel))
        {
            PlotHost.Cursor = Cursors.Arrow;
            return;
        }

        PlotHost.Cursor = HitTestDragTarget(colorBarRect, pixel) == ColorBarDragTarget.None
            ? Cursors.Arrow
            : Cursors.SizeNS;
    }

    private ColorBarDragTarget HitTestDragTarget(PixelRect rect, Pixel pixel)
    {
        var handleHeight = rect.Height * ColorBarHandleFraction;
        if (pixel.Y >= rect.Bottom - handleHeight)
            return ColorBarDragTarget.Min;

        if (pixel.Y <= rect.Top + handleHeight)
            return ColorBarDragTarget.Max;

        return ColorBarDragTarget.None;
    }

    private static double PixelYToColorValue(PixelRect rect, float pixelY, double colorMin, double colorMax)
    {
        var t = (rect.Bottom - pixelY) / rect.Height;
        t = Math.Clamp(t, 0f, 1f);
        return colorMin + t * (colorMax - colorMin);
    }

    private static double GetMinimumColorSpan(HeatmapChartViewModel viewModel)
    {
        var span = Math.Max(Math.Abs(viewModel.DataMax - viewModel.DataMin), 1e-9);
        return span * MinColorRangeSpanRatio;
    }

    private void ApplyColorRangeToHeatmap(double min, double max)
    {
        if (_heatmap == null)
            return;

        _heatmap.ManualRange = new ScottPlot.Range(min, max);
        PlotHost.Refresh();
    }

    private PixelRect? TryGetColorBarBitmapRect()
    {
        var render = PlotHost.Plot.LastRender;
        var colorBar = PlotHost.Plot.Axes.GetPanels().OfType<ColorBar>().FirstOrDefault();
        if (colorBar == null)
            return null;

        var dataRect = render.DataRect;
        render.Layout.PanelOffsets.TryGetValue(colorBar, out var offset);

        return colorBar.Edge switch
        {
            Edge.Right => new PixelRect(
                dataRect.Right + offset,
                dataRect.Right + colorBar.Width + offset,
                dataRect.Bottom,
                dataRect.Top),
            Edge.Left => new PixelRect(
                dataRect.Left - offset - colorBar.Width,
                dataRect.Left - offset,
                dataRect.Bottom,
                dataRect.Top),
            _ => null
        };
    }

    private void Redraw()
    {
        if (!IsLoaded)
            return;

        var model = ViewModel?.RenderModel;
        if (model == null)
            return;

        PlotHost.Plot.Clear();
        RemoveColorBarPanels(PlotHost.Plot);
        ScottPlotStyle.ApplyMacTheme(PlotHost.Plot);
        _heatmap = null;

        if (model.XAxis.Length < 2 || model.YAxis.Length < 2)
        {
            PlotHost.Refresh();
            return;
        }

        _heatmap = PlotHost.Plot.Add.Heatmap(model.Values);
        _heatmap.Smooth = true;
        _heatmap.FlipVertically = true;
        if (model.UseLogYAxis)
        {
            var yMin = model.YAxis[0];
            var yMax = model.YAxis[^1];
            _heatmap.Rectangle = new CoordinateRect(
                model.XAxis[0],
                model.XAxis[^1],
                Math.Log10(yMin),
                Math.Log10(yMax));
            ConfigureLogYAxisFromCenters(model.YAxis);
        }
        else
        {
            _heatmap.Rectangle = new CoordinateRect(
                model.XAxis[0],
                model.XAxis[^1],
                model.YAxis[0],
                model.YAxis[^1]);
        }

        _heatmap.Colormap = new ScottPlot.Colormaps.Turbo();

        if (model.ColorMin is double min && model.ColorMax is double max)
            _heatmap.ManualRange = new ScottPlot.Range(min, max);

        PlotHost.Plot.Add.ColorBar(_heatmap);
        PlotHost.Plot.Axes.Bottom.Label.Text = model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = model.YLabel;
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();

        if (ViewModel?.TryGetColorRange(out _dragMin, out _dragMax) == true)
            return;

        if (ViewModel != null)
        {
            _dragMin = ViewModel.DataMin;
            _dragMax = ViewModel.DataMax;
        }
    }

    private static void RemoveColorBarPanels(Plot plot)
    {
        foreach (var panel in plot.Axes.GetPanels().OfType<ColorBar>().ToList())
            plot.Axes.Remove(panel);
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

    private enum ColorBarDragTarget
    {
        None,
        Min,
        Max
    }
}
