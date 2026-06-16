namespace TdmsViewer.Analysis.Parameters;

internal static class AnalysisStepParameterCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AnalysisParameterDefinition>> Definitions =
        new Dictionary<string, IReadOnlyList<AnalysisParameterDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Waveform"] = Waveform(),
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

    public static string GetCalcValueDescription(string? calcType) =>
        calcType switch
        {
            "FrameLength" => "帧长度 (s)",
            "SpectrumLines" => "FFT 谱线数量",
            _ => "频率分辨率 (Hz)"
        };

    public static string GetStepValueDescription(string? stepType) =>
        string.Equals(stepType, "Overlap", StringComparison.OrdinalIgnoreCase)
            ? "重叠率 (0~1)"
            : "时间增量 (s)";

    public static string? GetChoiceLabel(string stepType, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var definition = Get(stepType).FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

        return definition?.Choices.FirstOrDefault(c =>
            string.Equals(c.Value, value, StringComparison.OrdinalIgnoreCase))?.Label;
    }

    private static IReadOnlyList<AnalysisParameterDefinition> Waveform() =>
    [
        Int("maxPoints", "显示点数", 2000, "降采样后每条曲线最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> OverallLevel() =>
    [
        Int("spectrumLines", "谱线数", 4096, "FFT 谱线数量"),
        Double("increment", "时间增量 (s)", 0.2, "总声级曲线步进间隔"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("window", "窗函数", "Hanning", WindowChoices()),
        Choice("weight", "计权", "A", WeightChoices()),
        Choice("scale", "刻度", "Db", ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> AveragedSpectrum() =>
    [
        Choice("calcType", "计算类型", "Resolution", CalcTypeChoices()),
        Double("calcValue", "计算值", 1.0, "与计算类型对应的数值"),
        Choice("stepType", "步进类型", "Increment", StepTypeChoices()),
        Double("stepValue", "步进值", 0.15, "重叠率或时间增量"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", FormatChoices()),
        Choice("average", "平均方式", "Energy", AverageChoices()),
        Choice("window", "窗函数", "Hanning", WindowChoices()),
        Choice("weight", "计权", "A", WeightChoices()),
        Choice("scale", "刻度", "Db", ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> OctaveBands() =>
    [
        Int("spectrumLines", "谱线数", 4096, "平均谱 FFT 谱线数量"),
        Double("overlap", "重叠率", 0.5, "谱图重叠比例 (0~1)"),
        Choice("octave", "倍频程", "ThirdOctave", OctaveChoices()),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", FormatChoices()),
        Choice("average", "平均方式", "Energy", AverageChoices()),
        Choice("window", "窗函数", "Hanning", WindowChoices()),
        Choice("weight", "计权", "Linear", WeightChoices())
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
        IReadOnlyList<AnalysisChoiceOption> choices,
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

    private static IReadOnlyList<AnalysisChoiceOption> WindowChoices() =>
    [
        new() { Value = "Uniform", Label = "矩形窗" },
        new() { Value = "Hanning", Label = "汉宁窗" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> WeightChoices() =>
    [
        new() { Value = "Linear", Label = "线性" },
        new() { Value = "A", Label = "A 计权" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> ScaleChoices() =>
    [
        new() { Value = "Linear", Label = "线性" },
        new() { Value = "Db", Label = "分贝 (dB)" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> FormatChoices() =>
    [
        new() { Value = "Rms", Label = "RMS" },
        new() { Value = "Peak", Label = "峰值" },
        new() { Value = "Peak2Peak", Label = "峰峰值" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> AverageChoices() =>
    [
        new() { Value = "Energy", Label = "能量平均" },
        new() { Value = "Mean", Label = "算术平均" },
        new() { Value = "Max", Label = "最大值" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> CalcTypeChoices() =>
    [
        new() { Value = "Resolution", Label = "频率分辨率" },
        new() { Value = "FrameLength", Label = "帧长度" },
        new() { Value = "SpectrumLines", Label = "谱线数" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> StepTypeChoices() =>
    [
        new() { Value = "Overlap", Label = "重叠率" },
        new() { Value = "Increment", Label = "时间增量" }
    ];

    private static IReadOnlyList<AnalysisChoiceOption> OctaveChoices() =>
    [
        new() { Value = "Octave", Label = "全倍频程" },
        new() { Value = "ThirdOctave", Label = "1/3 倍频程" },
        new() { Value = "SixthOctave", Label = "1/6 倍频程" },
        new() { Value = "TwelfthOctave", Label = "1/12 倍频程" },
        new() { Value = "TwentyFourthOctave", Label = "1/24 倍频程" }
    ];
}
