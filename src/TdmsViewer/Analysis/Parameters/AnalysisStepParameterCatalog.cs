namespace TdmsViewer.Analysis.Parameters;

internal static class AnalysisStepParameterCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AnalysisParameterDefinition>> Definitions =
        MergeDefinitions();

    private static Dictionary<string, IReadOnlyList<AnalysisParameterDefinition>> MergeDefinitions()
    {
        var definitions = new Dictionary<string, IReadOnlyList<AnalysisParameterDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Waveform"] = Waveform(),
            ["OverallLevel"] = OverallLevel(),
            ["AveragedSpectrum"] = AveragedSpectrum(),
            ["OctaveBands"] = OctaveBands()
        };

        foreach (var (key, value) in AnalysisStepParameterCatalogExtended.All)
            definitions[key] = value;

        return definitions;
    }

    public static IReadOnlyList<AnalysisParameterDefinition> Get(string stepType)
    {
        if (!Definitions.TryGetValue(stepType, out var definitions))
            return Array.Empty<AnalysisParameterDefinition>();

        return [..definitions, ..SharedTimeRangeParams()];
    }

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

    private static IReadOnlyList<AnalysisParameterDefinition> AveragedSpectrum() => SharedAveragedSpectrumParams();

    internal static IReadOnlyList<AnalysisParameterDefinition> SharedAveragedSpectrumParams() =>
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

    internal static IReadOnlyList<AnalysisParameterDefinition> SharedMapSpectralParams() =>
    [
        Int("spectrumLines", "谱线数", 4096, "FFT 谱线数量"),
        Double("increment", "时间增量 (s)", 0.15, "谱图步进间隔"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", FormatChoices()),
        Choice("window", "窗函数", "Hanning", WindowChoices()),
        Choice("weight", "计权", "A", WeightChoices()),
        Choice("scale", "刻度", "Db", ScaleChoices())
    ];

    internal static IReadOnlyList<AnalysisParameterDefinition> SharedRpmChannelParams() =>
    [
        Text("rpmGroupName", "转速通道组", "", "留空则使用信号通道所在组"),
        Text("rpmChannelName", "转速通道名", "", "TDMS 中转速/tacho 通道名称"),
        Double("rpmSampleRateHz", "转速采样率 (Hz)", 0, "0 表示与信号通道相同")
    ];

    internal static IReadOnlyList<AnalysisParameterDefinition> SharedRpmAxisParams() =>
    [
        Double("minRpm", "最小转速", 600, "转速轴下限 (RPM)"),
        Double("maxRpm", "最大转速", 4000, "转速轴上限 (RPM)"),
        Double("rpmStep", "转速步长", 25, "转速轴步进 (RPM)"),
        Choice("rpmTrigger", "转速触发", "Up", RpmTriggerChoices())
    ];

    internal static IReadOnlyList<AnalysisParameterDefinition> SharedTimeRangeParams() =>
    [
        Double("startTimeSec", "起始时间 (s)", 0, "0 表示信号开头；留 0 可继承方案全局时段"),
        Double("endTimeSec", "结束时间 (s)", 0, "0 表示信号末尾；留 0 可继承方案全局时段")
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

    internal static AnalysisParameterDefinition Int(string key, string name, int value, string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Integer,
            DefaultValue = value
        };

    internal static AnalysisParameterDefinition Double(string key, string name, double value, string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Double,
            DefaultValue = value
        };

    internal static AnalysisParameterDefinition Scientific(string key, string name, double value, string? description = null) =>
        Double(key, name, value, description);

    internal static AnalysisParameterDefinition Text(string key, string name, string value, string? description = null) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Description = description,
            Kind = AnalysisParameterKind.Text,
            DefaultValue = value
        };

    internal static AnalysisParameterDefinition Choice(
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

    internal static IReadOnlyList<AnalysisChoiceOption> WindowChoices() =>
    [
        new() { Value = "Uniform", Label = "矩形窗" },
        new() { Value = "Hanning", Label = "汉宁窗" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> WeightChoices() =>
    [
        new() { Value = "Linear", Label = "线性" },
        new() { Value = "A", Label = "A 计权" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> ScaleChoices() =>
    [
        new() { Value = "Linear", Label = "线性" },
        new() { Value = "Db", Label = "分贝 (dB)" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> FormatChoices() =>
    [
        new() { Value = "Rms", Label = "RMS" },
        new() { Value = "Peak", Label = "峰值" },
        new() { Value = "Peak2Peak", Label = "峰峰值" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> AverageChoices() =>
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

    private static IReadOnlyList<AnalysisChoiceOption> RpmTriggerChoices() =>
    [
        new() { Value = "Up", Label = "上升沿" },
        new() { Value = "ImmUp", Label = "即时上升" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> SoundFieldChoices() =>
    [
        new() { Value = "Free", Label = "自由场" },
        new() { Value = "Diffuse", Label = "扩散场" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> SharpnessWeightingChoices() =>
    [
        new() { Value = "Din", Label = "DIN" },
        new() { Value = "Aures", Label = "Aures" },
        new() { Value = "Bismark", Label = "Bismark" },
        new() { Value = "Fastl", Label = "Fastl" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> FluctuationMethodChoices() =>
    [
        new() { Value = "Stationary", Label = "稳态" },
        new() { Value = "TimeVarying", Label = "时变" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> ResamplerPlanningChoices() =>
    [
        new() { Value = "Fast", Label = "快速" },
        new() { Value = "Balanced", Label = "均衡" },
        new() { Value = "Patient", Label = "精细" }
    ];

    internal static IReadOnlyList<AnalysisChoiceOption> FrequencyAxisChoices() =>
    [
        new() { Value = "Linear", Label = "线性" },
        new() { Value = "Log", Label = "对数" }
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
