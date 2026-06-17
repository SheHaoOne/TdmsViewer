using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.ViewModels;

public sealed partial class HeatmapChartViewModel : ObservableObject
{
    public HeatmapChartViewModel(HeatmapChartModel source)
    {
        Source = source;
        RenderModel = source;
        (DataMin, DataMax) = PlotDataHelper.GetMatrixRange(source.Values);

        if (source.ColorMin is double min && source.ColorMax is double max)
        {
            UseAutoColorRange = false;
            ColorMinText = FormatValue(min);
            ColorMaxText = FormatValue(max);
        }
        else
        {
            ColorMinText = FormatValue(DataMin);
            ColorMaxText = FormatValue(DataMax);
        }

        UpdateRenderModel();
    }

    public HeatmapChartModel Source { get; }

    public string Title => Source.Title;

    public double DataMin { get; }

    public double DataMax { get; }

    [ObservableProperty]
    private bool _useAutoColorRange = true;

    [ObservableProperty]
    private string _colorMinText = string.Empty;

    [ObservableProperty]
    private string _colorMaxText = string.Empty;

    [ObservableProperty]
    private string? _colorRangeSummary;

    [ObservableProperty]
    private HeatmapViewMode _viewMode = HeatmapViewMode.Flat2D;

    public bool Is2DView
    {
        get => ViewMode == HeatmapViewMode.Flat2D;
        set
        {
            if (value)
                ViewMode = HeatmapViewMode.Flat2D;
        }
    }

    public bool Is3DView
    {
        get => ViewMode == HeatmapViewMode.Surface3D;
        set
        {
            if (value)
                ViewMode = HeatmapViewMode.Surface3D;
        }
    }

    public HeatmapChartModel RenderModel { get; private set; }

    partial void OnViewModeChanged(HeatmapViewMode value)
    {
        OnPropertyChanged(nameof(Is2DView));
        OnPropertyChanged(nameof(Is3DView));
    }

    partial void OnUseAutoColorRangeChanged(bool value)
    {
        if (value)
        {
            ColorMinText = FormatValue(DataMin);
            ColorMaxText = FormatValue(DataMax);
        }

        UpdateRenderModel();
    }

    partial void OnColorMinTextChanged(string value) => UpdateRenderModel();

    partial void OnColorMaxTextChanged(string value) => UpdateRenderModel();

    [RelayCommand]
    private void ResetColorRange()
    {
        UseAutoColorRange = true;
        ColorMinText = FormatValue(DataMin);
        ColorMaxText = FormatValue(DataMax);
        UpdateRenderModel();
    }

    private void UpdateRenderModel()
    {
        RenderModel = BuildRenderModel();
        OnPropertyChanged(nameof(RenderModel));
        ColorRangeSummary = BuildSummary(RenderModel);
    }

    private HeatmapChartModel BuildRenderModel()
    {
        if (UseAutoColorRange)
            return Source with { ColorMin = null, ColorMax = null };

        if (!TryParseColorRange(out var min, out var max))
            return Source with { ColorMin = null, ColorMax = null };

        return Source with { ColorMin = min, ColorMax = max };
    }

    private string BuildSummary(HeatmapChartModel model)
    {
        if (UseAutoColorRange)
            return $"色阶：自动（{FormatValue(DataMin)} ~ {FormatValue(DataMax)}）";

        if (model.ColorMin is not double min || model.ColorMax is not double max)
            return "色阶：请输入有效数值，且上限大于下限";

        return $"色阶：手动（{FormatValue(min)} ~ {FormatValue(max)}）";
    }

    private bool TryParseColorRange(out double min, out double max)
    {
        min = 0;
        max = 0;
        if (!double.TryParse(ColorMinText, NumberStyles.Float, CultureInfo.InvariantCulture, out min))
            return false;

        if (!double.TryParse(ColorMaxText, NumberStyles.Float, CultureInfo.InvariantCulture, out max))
            return false;

        return max > min;
    }

    public bool TryGetColorRange(out double min, out double max)
    {
        if (UseAutoColorRange)
        {
            min = DataMin;
            max = DataMax;
            return true;
        }

        return TryParseColorRange(out min, out max);
    }

    public void ApplyDraggedColorRange(double min, double max)
    {
        if (UseAutoColorRange)
            UseAutoColorRange = false;

        ColorMinText = FormatValue(min);
        ColorMaxText = FormatValue(max);
        ColorRangeSummary = $"色阶：手动（{FormatValue(min)} ~ {FormatValue(max)}）· 拖拽色条调整";
    }

    public void CommitManualColorRange(double min, double max)
    {
        UseAutoColorRange = false;
        ColorMinText = FormatValue(min);
        ColorMaxText = FormatValue(max);
        UpdateRenderModel();
    }

    private static string FormatValue(double value) =>
        value.ToString("G", CultureInfo.InvariantCulture);
}
