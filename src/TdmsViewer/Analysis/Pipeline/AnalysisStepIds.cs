namespace TdmsViewer.Analysis.Pipeline;

public static class AnalysisStepIds
{
    public static readonly IReadOnlyDictionary<string, string> DefaultBlockIds =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Spl"] = "spl",
            ["TimeDomain"] = "td",
            ["Waveform"] = "wf",
            ["Spectrum"] = "sp",
            ["OctaveBands"] = "ob",
            ["Psd"] = "psd",
            ["Stft"] = "stft",
            ["Wavelet"] = "cwt"
        };

    public static string GetBlockId(string stepType) =>
        DefaultBlockIds.TryGetValue(stepType, out var id)
            ? id
            : stepType.ToLowerInvariant();
}
