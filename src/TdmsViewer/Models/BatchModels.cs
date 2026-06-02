namespace TdmsViewer.Models;

public sealed class TdmsFileEntry
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required IReadOnlyList<TdmsChannelInfo> Channels { get; init; }
}

public sealed class ChannelSourceRef
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required TdmsChannelInfo Channel { get; init; }
}

/// <summary>
/// 跨文件合并后的通道（相同组名 + 通道名视为同一通道）。
/// </summary>
public sealed class MergedChannelInfo
{
    public required string ChannelKey { get; init; }
    public required string GroupName { get; init; }
    public required string ChannelName { get; init; }
    public required string DisplayName { get; init; }
    public int SourceCount => Sources.Count;
    public required IReadOnlyList<ChannelSourceRef> Sources { get; init; }
}

public sealed class WaveformSeries
{
    public required string Label { get; init; }
    public required string Color { get; init; }
    public IReadOnlyList<WaveformPoint> Points { get; init; } = Array.Empty<WaveformPoint>();
}
