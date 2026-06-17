namespace TdmsViewer.Analysis.Pipeline;

using TdmsViewer.Analysis.Contracts;

public sealed class AnalysisPlanStep
{
    public required string StepType { get; init; }
    public required string Id { get; init; }
    public bool Enabled { get; init; } = true;
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
        new Dictionary<string, object?>();
}

public sealed class AnalysisPlan
{
    public string Name { get; init; } = "快速声学检查";
    public AnalysisTimeRange? GlobalTimeRange { get; init; }
    public IReadOnlyList<AnalysisPlanStep> Steps { get; init; } = Array.Empty<AnalysisPlanStep>();

    public static AnalysisPlan CreateDefault() => new()
    {
        Name = "快速声学检查",
        Steps =
        [
            new AnalysisPlanStep { Id = "wf", StepType = "Waveform" },
            new AnalysisPlanStep
            {
                Id = "spl",
                StepType = "OverallLevel",
                Parameters = new Dictionary<string, object?>
                {
                    ["spectrumLines"] = 4096,
                    ["increment"] = 0.2,
                    ["referenceValue"] = 2.0e-5,
                    ["window"] = "Hanning",
                    ["weight"] = "A",
                    ["scale"] = "Db"
                }
            },
            new AnalysisPlanStep
            {
                Id = "as",
                StepType = "AveragedSpectrum",
                Parameters = new Dictionary<string, object?>
                {
                    ["calcType"] = "Resolution",
                    ["calcValue"] = 1.0,
                    ["stepType"] = "Increment",
                    ["stepValue"] = 0.15,
                    ["referenceValue"] = 2.0e-5,
                    ["format"] = "Rms",
                    ["average"] = "Energy",
                    ["window"] = "Hanning",
                    ["weight"] = "A",
                    ["scale"] = "Db"
                }
            },
            new AnalysisPlanStep
            {
                Id = "ob",
                StepType = "OctaveBands",
                Parameters = new Dictionary<string, object?>
                {
                    ["spectrumLines"] = 4096,
                    ["overlap"] = 0.5,
                    ["octave"] = "ThirdOctave",
                    ["referenceValue"] = 2.0e-5
                }
            }
        ]
    };
}
