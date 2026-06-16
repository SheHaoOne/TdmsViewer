namespace TdmsViewer.Analysis.Parameters;

internal static class AnalysisStepParameterCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AnalysisParameterDefinition>> Definitions =
        new Dictionary<string, IReadOnlyList<AnalysisParameterDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["OverallLevel"] = OverallLevel(),
            ["AveragedSpectrum"] = AveragedSpectrum(),
            ["OctaveBands"] = OctaveBands()
        };

    public static IReadOnlyList<AnalysisParameterDefinition> Get(string stepType) =>
        Definitions.TryGetValue(stepType, out var definitions)
            ? definitions
            : Array.Empty<AnalysisParameterDefinition>();

    public static IReadOnlyDictionary<string, object?> GetDefaults(string stepType) =>
        Get(stepType).ToDictionary(d => d.Key, d => (object?)d.DefaultValue, StringComparer.Ordinal);

    private static IReadOnlyList<AnalysisParameterDefinition> OverallLevel() =>
    [
        Int("spectrumLines", "谱线数", 4096, "FFT 谱线数量"),
        Double("increment", "时间增量 (s)", 0.2, "总声级曲线步进间隔"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("window", "窗函数", "Hanning", ["Uniform", "Hanning"]),
        Choice("weight", "计权", "A", ["Linear", "A"]),
        Choice("scale", "刻度", "Db", ["Linear", "Db"])
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> AveragedSpectrum() =>
    [
        Choice("calcType", "计算类型", "Resolution", ["Resolution", "FrameLength", "SpectrumLines"]),
        Double("calcValue", "计算值", 1.0, "与计算类型对应的数值"),
        Choice("stepType", "步进类型", "Increment", ["Overlap", "Increment"]),
        Double("stepValue", "步进值", 0.15, "重叠率或时间增量"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", ["Rms", "Peak", "Peak2Peak"]),
        Choice("average", "平均方式", "Energy", ["Energy", "Mean", "Max"]),
        Choice("window", "窗函数", "Hanning", ["Uniform", "Hanning"]),
        Choice("weight", "计权", "A", ["Linear", "A"]),
        Choice("scale", "刻度", "Db", ["Linear", "Db"])
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> OctaveBands() =>
    [
        Int("spectrumLines", "谱线数", 4096, "平均谱 FFT 谱线数量"),
        Double("overlap", "重叠率", 0.5, "谱图重叠比例 (0~1)"),
        Choice("octave", "倍频程", "ThirdOctave",
            ["Octave", "ThirdOctave", "SixthOctave", "TwelfthOctave", "TwentyFourthOctave"]),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压")
    ];

    private static AnalysisParameterDefinition Int(string key, string name, int value, string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Integer,
            DefaultValue = value
        };

    private static AnalysisParameterDefinition Double(string key, string name, double value, string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Double,
            DefaultValue = value
        };

    private static AnalysisParameterDefinition Scientific(string key, string name, double value, string? description = null) =>
        Double(key, name, value, description);

    private static AnalysisParameterDefinition Choice(
        string key,
        string name,
        string defaultValue,
        IReadOnlyList<string> choices,
        string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Choice,
            DefaultValue = defaultValue,
            Choices = choices
        };
}
