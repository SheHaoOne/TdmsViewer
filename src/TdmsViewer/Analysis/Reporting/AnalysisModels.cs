namespace TdmsViewer.Analysis.Reporting;

public enum BlockStatus
{
    Ok,
    Warning,
    Fail,
    Info
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
}

public sealed class ReportBlock
{
    public required string BlockId { get; init; }
    public required string WidgetType { get; init; }
    public required string Title { get; init; }
    public required object Payload { get; init; }
    public BlockStatus Status { get; init; } = BlockStatus.Ok;
}

public sealed class WidgetPlacement
{
    public required string BlockId { get; init; }
    public required string WidgetType { get; init; }
    public int Col { get; init; }
    public int Row { get; init; }
    public int ColSpan { get; init; } = 6;
    public int RowSpan { get; init; } = 2;
}

public sealed class DashboardLayout
{
    public int Columns { get; init; } = 24;
    public IReadOnlyList<WidgetPlacement> Widgets { get; init; } = Array.Empty<WidgetPlacement>();
}

public sealed class AnalysisReport
{
    public required ReportMeta Meta { get; init; }
    public required IReadOnlyList<ReportBlock> Blocks { get; init; }
    public required DashboardLayout Layout { get; init; }
}
