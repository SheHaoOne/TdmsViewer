namespace TdmsViewer.Models;

public sealed class TdmsGroupInfo
{
    public required string GroupName { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
    public required IReadOnlyList<TdmsChannelInfo> Channels { get; init; }
    public int ChannelCount => Channels.Count;
}
