namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisPlanStep
{
    public required string StepType { get; init; }
    public required string Id { get; init; }
    public bool Enabled { get; init; } = true;
}

public sealed class AnalysisPlan
{
    public string Name { get; init; } = "NVH 声学分析";
    public string DashboardTemplateId { get; init; } = "nvh-acoustic-light";
    public IReadOnlyList<AnalysisPlanStep> Steps { get; init; } = Array.Empty<AnalysisPlanStep>();

    public static AnalysisPlan CreateDefault() => new()
    {
        Steps =
        [
            new AnalysisPlanStep { Id = "spl", StepType = "Spl" },
            new AnalysisPlanStep { Id = "td", StepType = "TimeDomain" },
            new AnalysisPlanStep { Id = "wf", StepType = "Waveform" },
            new AnalysisPlanStep { Id = "sp", StepType = "Spectrum" },
            new AnalysisPlanStep { Id = "ob", StepType = "OctaveBands" }
        ]
    };
}
