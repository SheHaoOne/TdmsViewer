namespace TdmsViewer.Analysis.Contracts;

public sealed class AnalysisTimeRange
{
    public double StartSec { get; init; }

    /// <summary>Null or 0 means analyze through the end of the available signal.</summary>
    public double? EndSec { get; init; }

    public static AnalysisTimeRange Full { get; } = new() { StartSec = 0, EndSec = null };

    public double ResolveEndSec(double totalDurationSec)
    {
        var start = Math.Clamp(StartSec, 0, totalDurationSec);
        if (EndSec is not > 0)
            return totalDurationSec;

        return Math.Clamp(EndSec.Value, start, totalDurationSec);
    }

    public double Duration(double totalDurationSec)
    {
        var start = Math.Clamp(StartSec, 0, totalDurationSec);
        return Math.Max(0, ResolveEndSec(totalDurationSec) - start);
    }

    public bool IsFullSegment(double totalDurationSec) =>
        StartSec <= 0 && (EndSec is null or <= 0 || EndSec >= totalDurationSec);

    public string FormatSummary(double totalDurationSec)
    {
        var start = Math.Clamp(StartSec, 0, totalDurationSec);
        var end = ResolveEndSec(totalDurationSec);
        return $"{start:F3} – {end:F3} s（共 {end - start:F3} s / 全长 {totalDurationSec:F3} s）";
    }
}
