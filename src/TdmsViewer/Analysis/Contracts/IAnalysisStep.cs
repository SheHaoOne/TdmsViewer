using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Contracts;

public sealed class AnalysisInputContext
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string GroupName { get; init; }
    public required string ChannelName { get; init; }
    public required double[] Samples { get; init; }
    public required double SampleRateHz { get; init; }
    public required IReadOnlyList<AnalysisSourceSample> Sources { get; init; }
    public IAnalysisDataAccessor? Data { get; init; }
    public AnalysisTimeRange? GlobalTimeRange { get; init; }
}

public sealed class StepDefinition
{
    public required string StepType { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
}

public interface IAnalysisStep
{
    string StepType { get; }
    StepDefinition Definition { get; }
    Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken);
}
