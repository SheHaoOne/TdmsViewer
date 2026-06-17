namespace TdmsViewer.Analysis.Reporting;

public enum BlockStatus
{
    Ok,
    Warning,
    Fail,
    Info
}

public enum ChartKind
{
    Line,
    Bar,
    Heatmap,
    Metric
}

public sealed class ReportMeta
{
    public required string Title { get; init; }
    public required string FileName { get; init; }
    public required string ChannelName { get; init; }
    public required string GroupName { get; init; }
    public double SampleRateHz { get; init; }
    public int SampleCount { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string? PlanName { get; init; }
    public double? AnalyzedStartSec { get; init; }
    public double? AnalyzedEndSec { get; init; }
    public string? TimeRangeSummary { get; init; }
}

public abstract record ChartCardModel(string Id, string Title, ChartKind Kind);

public sealed class LineSeriesData
{
    public required string Label { get; init; }
    public required double[] X { get; init; }
    public required double[] Y { get; init; }
    public string Color { get; init; } = "#007AFF";
    public string? SourceKey { get; init; }
}

public sealed record LineChartModel(
    string Id,
    string Title,
    string XLabel,
    string YLabel,
    IReadOnlyList<LineSeriesData> Series,
    bool UseLogXAxis = false,
    bool UseStepLine = false
) : ChartCardModel(Id, Title, ChartKind.Line);

public sealed class BarSeriesData
{
    public required string Label { get; init; }
    public required double[] Values { get; init; }
    public string Color { get; init; } = "#007AFF";
    public string? SourceKey { get; init; }
}

public sealed record BarChartModel(
    string Id,
    string Title,
    string XLabel,
    string YLabel,
    double[] Categories,
    double[] Values,
    string[]? CategoryLabels = null,
    IReadOnlyList<BarSeriesData>? Series = null,
    string? SourceKey = null
) : ChartCardModel(Id, Title, ChartKind.Bar);

public sealed class ReportSourceInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string Color { get; init; }
}

public sealed record MetricCardModel(
    string Id,
    string Title,
    string Value,
    string Unit,
    BlockStatus Status = BlockStatus.Ok,
    string? SourceKey = null
) : ChartCardModel(Id, Title, ChartKind.Metric);

public sealed record HeatmapChartModel(
    string Id,
    string Title,
    string XLabel,
    string YLabel,
    double[,] Values,
    double[] XAxis,
    double[] YAxis,
    string? SourceKey = null,
    double? ColorMin = null,
    double? ColorMax = null,
    bool UseLogYAxis = false
) : ChartCardModel(Id, Title, ChartKind.Heatmap);

public sealed class AnalysisReportModel
{
    public required ReportMeta Meta { get; init; }
    public required IReadOnlyList<ChartCardModel> Cards { get; init; }
    public required IReadOnlyList<ReportSourceInfo> Sources { get; init; }
}
