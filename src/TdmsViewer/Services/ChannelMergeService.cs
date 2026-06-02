using TdmsViewer.Models;

namespace TdmsViewer.Services;

public sealed class ChannelMergeService
{
    public static string BuildChannelKey(string groupName, string channelName) =>
        $"{groupName}\u001f{channelName}";

    public IReadOnlyList<MergedChannelInfo> MergeChannels(IEnumerable<TdmsFileEntry> files)
    {
        var map = new Dictionary<string, List<ChannelSourceRef>>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            foreach (var channel in file.Channels)
            {
                var key = BuildChannelKey(channel.GroupName, channel.ChannelName);
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
            .OrderBy(p => p.Value[0].Channel.GroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Value[0].Channel.ChannelName, StringComparer.OrdinalIgnoreCase)
            .Select(p =>
            {
                var first = p.Value[0].Channel;
                return new MergedChannelInfo
                {
                    ChannelKey = p.Key,
                    GroupName = first.GroupName,
                    ChannelName = first.ChannelName,
                    DisplayName = $"{first.GroupName} / {first.ChannelName}",
                    Sources = p.Value.OrderBy(s => s.FileName, StringComparer.OrdinalIgnoreCase).ToList()
                };
            })
            .ToList();
    }
}
