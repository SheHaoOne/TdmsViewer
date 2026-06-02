namespace TdmsViewer.Models;

public sealed class TdmsChannelInfo
{
    public required string GroupName { get; init; }
    public required string ChannelName { get; init; }
    public required string DisplayName { get; init; }
    public required string DataTypeName { get; init; }
    public long SampleCount { get; init; }
    public double? SampleRateHz { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
}

public sealed class ChannelPropertyCard
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public sealed class DataPageRow
{
    public long Index { get; init; }
    public double Value { get; init; }
    public string FormattedValue { get; init; } = string.Empty;
}

public sealed class WaveformPoint
{
    public double X { get; init; }
    public double Y { get; init; }
}
