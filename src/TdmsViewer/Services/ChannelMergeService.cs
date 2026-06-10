using TdmsViewer.Models;

namespace TdmsViewer.Services;

public sealed class ChannelMergeService
{
    public static string BuildChannelKey(string channelName) => channelName;

    public IReadOnlyList<MergedChannelInfo> MergeChannels(
        IEnumerable<TdmsFileEntry> files,
        string groupName)
    {
        var map = new Dictionary<string, List<ChannelSourceRef>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            foreach (var channel in file.Channels)
            {
                if (!string.Equals(channel.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var key = BuildChannelKey(channel.ChannelName);
                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<ChannelSourceRef>();
                    map[key] = list;
                }

                list.Add(new ChannelSourceRef
                {
                    FilePath = file.FilePath,
                    FileName = file.FileName,
                    Channel = channel
                });
            }
        }

        return map
            .OrderBy(p => p.Value[0].Channel.ChannelName, StringComparer.OrdinalIgnoreCase)
            .Select(p => CreateMergedChannel(groupName, p.Key, p.Value))
            .ToList();
    }

    private static MergedChannelInfo CreateMergedChannel(
        string groupName,
        string channelKey,
        List<ChannelSourceRef> sources)
    {
        var ordered = sources.OrderBy(s => s.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        var first = ordered[0].Channel;

        return new MergedChannelInfo
        {
            ChannelKey = channelKey,
            GroupName = groupName,
            ChannelName = first.ChannelName,
            DisplayName = first.ChannelName,
            Sources = ordered
        };
    }
}
