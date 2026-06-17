using NvhLibCSharp.Interop;
using TdmsViewer.Analysis;
using TdmsViewer.Analysis.Contracts;

namespace TdmsViewer.Services;

public static class NvhSignalAdapter
{
    public static Signal ToSignal(double[] samples, double sampleRateHz, AnalysisTimeRange? range = null)
    {
        var segment = range == null || range.IsFullSegment(samples.Length / sampleRateHz)
            ? new SignalSegmentHelper.SliceResult(samples, 0, samples.Length / sampleRateHz, samples.Length / sampleRateHz)
            : SignalSegmentHelper.Slice(samples, sampleRateHz, range);

        return ToSignal(segment.Samples, sampleRateHz);
    }

    public static Signal ToSignal(double[] samples, double sampleRateHz)
    {
        if (samples.Length == 0)
            throw new InvalidOperationException("通道数据为空，无法进行分析。");

        if (sampleRateHz <= 0)
            throw new InvalidOperationException("采样率无效，无法进行分析。");

        return new Signal(samples, 1.0 / sampleRateHz);
    }
}
