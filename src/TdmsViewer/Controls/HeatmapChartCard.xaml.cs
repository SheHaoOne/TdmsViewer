using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.TickGenerators;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Controls;

public partial class HeatmapChartCard : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(HeatmapChartViewModel),
            typeof(HeatmapChartCard),
            new PropertyMetadata(null, OnViewModelChanged));

    public HeatmapChartCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public HeatmapChartViewModel? ViewModel
    {
        get => (HeatmapChartViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(HeatmapChartViewModel.RenderModel)
            or nameof(HeatmapChartViewModel.UseAutoColorRange))
        {
            Redraw();
        }
    }

    private void Redraw()
    {
        if (!IsLoaded)
            return;

        var model = ViewModel?.RenderModel;
        if (model == null)
            return;

        PlotHost.Plot.Clear();
        ScottPlotStyle.ApplyMacTheme(PlotHost.Plot);

        if (model.XAxis.Length < 2 || model.YAxis.Length < 2)
        {
            PlotHost.Refresh();
            return;
        }

        var heatmap = PlotHost.Plot.Add.Heatmap(model.Values);
        heatmap.Smooth = true;
        heatmap.FlipVertically = true;
        if (model.UseLogYAxis)
        {
            var yMin = model.YAxis[0];
            var yMax = model.YAxis[^1];
            heatmap.Rectangle = new CoordinateRect(
                model.XAxis[0],
                model.XAxis[^1],
                Math.Log10(yMin),
                Math.Log10(yMax));
            ConfigureLogYAxisFromCenters(model.YAxis);
        }
        else
        {
            heatmap.Rectangle = new CoordinateRect(
                model.XAxis[0],
                model.XAxis[^1],
                model.YAxis[0],
                model.YAxis[^1]);
        }

        heatmap.Colormap = new ScottPlot.Colormaps.Turbo();

        if (model.ColorMin is double min && model.ColorMax is double max)
            heatmap.ManualRange = new ScottPlot.Range(min, max);

        PlotHost.Plot.Add.ColorBar(heatmap);
        PlotHost.Plot.Axes.Bottom.Label.Text = model.XLabel;
        PlotHost.Plot.Axes.Left.Label.Text = model.YLabel;
        PlotHost.Plot.Axes.AutoScale();
        PlotHost.Refresh();
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
}
