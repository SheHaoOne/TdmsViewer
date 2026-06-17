namespace TdmsViewer.Analysis.Contracts;

public sealed class AnalysisSourceSample
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string GroupName { get; init; }
    public required string ChannelName { get; init; }
    public required double[] Samples { get; init; }
    public required double SampleRateHz { get; init; }
    public required string Label { get; init; }
    public string Color { get; init; } = "#007AFF";
}
