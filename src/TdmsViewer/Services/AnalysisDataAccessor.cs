using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Models;

namespace TdmsViewer.Services;

public sealed class AnalysisDataAccessor : IAnalysisDataAccessor
{
    private readonly TdmsFileService _tdmsService;
    private readonly IReadOnlyDictionary<string, TdmsFileEntry> _filesByPath;

    public AnalysisDataAccessor(
        TdmsFileService tdmsService,
        IEnumerable<TdmsFileEntry> loadedFiles)
    {
        _tdmsService = tdmsService;
        _filesByPath = loadedFiles.ToDictionary(
            f => f.FilePath,
            StringComparer.OrdinalIgnoreCase);
    }

    public double[]? TryReadChannel(string filePath, string groupName, string channelName)
    {
        if (!_filesByPath.TryGetValue(filePath, out var entry))
            return null;

        var channel = entry.Channels.FirstOrDefault(c =>
            string.Equals(c.GroupName, groupName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ChannelName, channelName, StringComparison.OrdinalIgnoreCase));

        if (channel == null)
            return null;

        try
        {
            return _tdmsService.ReadChannelData(filePath, channel);
        }
        catch
        {
            return null;
        }
    }
}
