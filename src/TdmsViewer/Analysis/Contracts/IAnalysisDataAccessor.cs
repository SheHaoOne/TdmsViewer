namespace TdmsViewer.Analysis.Contracts;

public interface IAnalysisDataAccessor
{
    double[]? TryReadChannel(string filePath, string groupName, string channelName);
}
