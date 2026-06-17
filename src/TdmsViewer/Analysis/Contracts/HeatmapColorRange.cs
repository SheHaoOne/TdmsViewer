namespace TdmsViewer.Analysis.Contracts;

public sealed class HeatmapColorRange
{
    public bool UseAuto { get; init; } = true;

    public double Min { get; init; }

    public double Max { get; init; }

    public static HeatmapColorRange CreateManual(double min, double max) =>
        new() { UseAuto = false, Min = min, Max = max };

    public string FormatSummary() =>
        UseAuto ? "热力图色阶：自动" : $"热力图色阶：{Min:G} ~ {Max:G}";
}
