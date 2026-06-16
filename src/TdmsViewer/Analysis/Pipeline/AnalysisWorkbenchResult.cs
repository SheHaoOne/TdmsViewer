using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisWorkbenchResult
{
    public required AnalysisReport PrimaryReport { get; init; }
    public required IReadOnlyList<AnalysisReport> AllReports { get; init; }
}
