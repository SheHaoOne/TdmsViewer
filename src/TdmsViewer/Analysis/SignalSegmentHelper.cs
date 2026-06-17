using TdmsViewer.Analysis.Contracts;

namespace TdmsViewer.Analysis;

public static class SignalSegmentHelper
{
    public sealed record SliceResult(double[] Samples, double StartSec, double EndSec, double DurationSec);

    public static SliceResult Slice(double[] samples, double sampleRateHz, AnalysisTimeRange range)
    {
        if (samples.Length == 0)
            return new SliceResult([], 0, 0, 0);

        if (sampleRateHz <= 0)
            throw new InvalidOperationException("采样率无效，无法切片。");

        var totalSec = samples.Length / sampleRateHz;
        var startSec = Math.Clamp(range.StartSec, 0, totalSec);
        var endSec = range.ResolveEndSec(totalSec);

        if (endSec <= startSec)
            throw new InvalidOperationException($"分析时段无效：起始 {startSec:F3} s，结束 {endSec:F3} s。");

        var startIndex = (int)Math.Floor(startSec * sampleRateHz);
        var endIndex = (int)Math.Ceiling(endSec * sampleRateHz);
        startIndex = Math.Clamp(startIndex, 0, samples.Length - 1);
        endIndex = Math.Clamp(endIndex, startIndex + 1, samples.Length);

        var length = endIndex - startIndex;
        var segment = new double[length];
        Array.Copy(samples, startIndex, segment, 0, length);

        var actualStart = startIndex / sampleRateHz;
        var actualEnd = endIndex / sampleRateHz;
        return new SliceResult(segment, actualStart, actualEnd, actualEnd - actualStart);
    }
}
