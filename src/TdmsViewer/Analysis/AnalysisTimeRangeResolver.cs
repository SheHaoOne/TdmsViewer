using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;

namespace TdmsViewer.Analysis;

public static class AnalysisTimeRangeResolver
{
    public static AnalysisTimeRange Resolve(
        AnalysisTimeRange? globalRange,
        IReadOnlyDictionary<string, object?>? stepParameters,
        double totalDurationSec)
    {
        var stepStart = StepParameters.GetDouble(stepParameters, "startTimeSec", 0);
        var stepEnd = StepParameters.GetDouble(stepParameters, "endTimeSec", 0);
        var stepOverrides = stepStart > 0 || stepEnd > 0;

        if (!stepOverrides && globalRange != null)
            return Normalize(globalRange, totalDurationSec);

        return Normalize(new AnalysisTimeRange
        {
            StartSec = stepStart,
            EndSec = stepEnd > 0 ? stepEnd : null
        }, totalDurationSec);
    }

    public static AnalysisTimeRange ResolveGlobal(
        double startSec,
        double endSec,
        double totalDurationSec) =>
        Normalize(new AnalysisTimeRange
        {
            StartSec = startSec,
            EndSec = endSec > 0 ? endSec : null
        }, totalDurationSec);

    public static string? Validate(double startSec, double endSec, double totalDurationSec, string contextLabel)
    {
        if (startSec < 0 || endSec < 0)
            return $"{contextLabel}：时间不能小于 0。";

        if (endSec > 0 && endSec <= startSec)
            return $"{contextLabel}：结束时间必须大于起始时间。";

        if (startSec > totalDurationSec)
            return $"{contextLabel}：起始时间超出通道时长（{totalDurationSec:F3} s）。";

        if (endSec > 0 && endSec > totalDurationSec)
            return $"{contextLabel}：结束时间超出通道时长（{totalDurationSec:F3} s）。";

        var range = new AnalysisTimeRange { StartSec = startSec, EndSec = endSec > 0 ? endSec : null };
        if (range.Duration(totalDurationSec) <= 0)
            return $"{contextLabel}：分析时段无效。";

        return null;
    }

    private static AnalysisTimeRange Normalize(AnalysisTimeRange range, double totalDurationSec)
    {
        var start = Math.Clamp(range.StartSec, 0, totalDurationSec);
        double? end = range.EndSec is > 0 ? Math.Clamp(range.EndSec.Value, start, totalDurationSec) : null;
        return new AnalysisTimeRange { StartSec = start, EndSec = end };
    }
}
