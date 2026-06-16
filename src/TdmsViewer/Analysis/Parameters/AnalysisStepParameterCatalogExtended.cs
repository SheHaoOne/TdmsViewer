namespace TdmsViewer.Analysis.Parameters;

internal static class AnalysisStepParameterCatalogExtended
{
    public static IReadOnlyDictionary<string, IReadOnlyList<AnalysisParameterDefinition>> All =>
        new Dictionary<string, IReadOnlyList<AnalysisParameterDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["TimeFrequencyMap"] = TimeFrequencyMap(),
            ["HilbertEnvelope"] = HilbertEnvelope(),
            ["HilbertEnvelopeSpectra"] = HilbertEnvelopeSpectra(),
            ["HilbertEnvelopeAvgSpectra"] = HilbertEnvelopeAvgSpectra(),
            ["HilbertEnvelopeExFixed"] = HilbertEnvelopeExFixed(),
            ["HilbertEnvelopeExTracked"] = HilbertEnvelopeExTracked(),
            ["MorletWavelet"] = MorletWavelet(),
            ["LmsMorletWavelet"] = LmsMorletWavelet(),
            ["ModulationSpectrum"] = ModulationSpectrum(),
            ["ModulationSpectrumStft"] = ModulationSpectrumStft(),
            ["StationaryLoudness"] = StationaryLoudness(),
            ["TimeVaryingLoudness"] = TimeVaryingLoudness(),
            ["StationarySharpness"] = StationarySharpness(),
            ["TimeVaryingSharpness"] = TimeVaryingSharpness(),
            ["Roughness"] = Roughness(),
            ["FluctuationStrength"] = FluctuationStrength(),
            ["Resample"] = Resample(),
            ["OrderSection"] = OrderSection(),
            ["RpmFrequencyMap"] = RpmFrequencyMap(),
            ["RpmOrderMap"] = RpmOrderMap()
        };

    private static IReadOnlyList<AnalysisParameterDefinition> TimeFrequencyMap() =>
    [
        ..AnalysisStepParameterCatalog.SharedMapSpectralParams(),
        Int("maxPoints", "时间轴显示点数", 500, "降采样后时间轴最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> HilbertEnvelope() =>
    [
        Int("maxPoints", "显示点数", 2000, "降采样后最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> HilbertEnvelopeSpectra() =>
    [
        Choice("window", "窗函数", "Hanning", AnalysisStepParameterCatalog.WindowChoices()),
        Choice("format", "幅值格式", "Rms", AnalysisStepParameterCatalog.FormatChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> HilbertEnvelopeAvgSpectra() =>
    [
        ..AnalysisStepParameterCatalog.SharedAveragedSpectrumParams()
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> HilbertEnvelopeExFixed() =>
    [
        Double("bandWidth", "带宽 (Hz)", 350, "固定带通带宽"),
        Double("centerFrequency", "中心频率 (Hz)", 875, "固定带通中心频率"),
        Int("maxPoints", "显示点数", 2000, "降采样后最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> HilbertEnvelopeExTracked() =>
    [
        Double("centerOrder", "中心阶次", 2.0, "跟踪带中心阶次"),
        Double("bandWidth", "带宽 (Hz)", 1000, "跟踪带带宽"),
        Int("windowLength", "窗口长度", 4096, "滑动窗口长度"),
        Double("minFrequency", "最小频率 (Hz)", 100, "跟踪带全局最小频率"),
        Double("maxFrequency", "最大频率 (Hz)", 4000, "跟踪带全局最大频率"),
        ..AnalysisStepParameterCatalog.SharedRpmChannelParams(),
        Int("maxPoints", "显示点数", 2000, "降采样后最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> MorletWavelet() =>
    [
        Double("minFrequency", "最小频率 (Hz)", 1.0, "分析最低频率"),
        Double("maxFrequency", "最大频率 (Hz)", 1000, "分析最高频率"),
        Int("bandCount", "频带数", 50, "对数分布频带数量"),
        Double("nCycles", "小波周期数", 5.0, "Morlet 小波循环数"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> LmsMorletWavelet() =>
    [
        Double("minFrequency", "最小频率 (Hz)", 10, "分析最低频率"),
        Double("maxFrequency", "最大频率 (Hz)", 1000, "分析最高频率"),
        Int("bandsPerOctave", "每倍频程频带数", 100, "对数频带分辨率"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> ModulationSpectrum() =>
    [
        Double("frequencyResolution", "频率分辨率 (Hz)", 1.0, "调制谱频率分辨率"),
        Double("cutoffFreq", "截止频率 (Hz)", 150.0, "调制频率截止"),
        Scientific("referenceValue", "参考值", 1.0 / 150, "dB 换算参考值"),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> ModulationSpectrumStft() =>
    [
        Int("windowSize", "窗口长度", 4096, "STFT 窗口长度"),
        Int("hopSize", "跳跃长度", 1024, "STFT 跳跃长度"),
        Double("cutoffFreq", "截止频率 (Hz)", 150.0, "调制频率截止"),
        Scientific("referenceValue", "参考值", 1.0 / 150, "dB 换算参考值"),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> StationaryLoudness() =>
    [
        Choice("soundField", "声场", "Free", AnalysisStepParameterCatalog.SoundFieldChoices()),
        Double("skipInSec", "跳过时长 (s)", 0.0, "分析前跳过的秒数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> TimeVaryingLoudness() =>
    [
        Choice("soundField", "声场", "Free", AnalysisStepParameterCatalog.SoundFieldChoices()),
        Double("skipInSec", "跳过时长 (s)", 0.0, "分析前跳过的秒数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> StationarySharpness() =>
    [
        Choice("weighting", "计权", "Din", AnalysisStepParameterCatalog.SharpnessWeightingChoices()),
        Choice("soundField", "声场", "Free", AnalysisStepParameterCatalog.SoundFieldChoices()),
        Double("skipInSec", "跳过时长 (s)", 0.0, "分析前跳过的秒数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> TimeVaryingSharpness() =>
    [
        Choice("weighting", "计权", "Din", AnalysisStepParameterCatalog.SharpnessWeightingChoices()),
        Choice("soundField", "声场", "Free", AnalysisStepParameterCatalog.SoundFieldChoices()),
        Double("skipInSec", "跳过时长 (s)", 0.0, "分析前跳过的秒数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> Roughness() =>
    [
        Choice("soundField", "声场", "Free", AnalysisStepParameterCatalog.SoundFieldChoices()),
        Double("skipInSec", "跳过时长 (s)", 0.3, "分析前跳过的秒数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> FluctuationStrength() =>
    [
        Choice("method", "方法", "Stationary", AnalysisStepParameterCatalog.FluctuationMethodChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> Resample() =>
    [
        Double("destSampleRateHz", "目标采样率 (Hz)", 44100, "重采样目标采样率"),
        Double("bandRatio", "带宽比", 0.9, "抗混叠带宽比 (0~1)"),
        Choice("planningMode", "规划模式", "Balanced", AnalysisStepParameterCatalog.ResamplerPlanningChoices()),
        Int("maxPoints", "显示点数", 2000, "降采样后最多绘制的点数")
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> OrderSection() =>
    [
        ..AnalysisStepParameterCatalog.SharedRpmChannelParams(),
        ..AnalysisStepParameterCatalog.SharedRpmAxisParams(),
        Int("spectrumLines", "谱线数", 4096, "FFT 谱线数量"),
        Double("targetOrder", "目标阶次", 14.0, "阶次截面中心阶次"),
        Double("orderBandwidth", "阶次带宽", 0.5, "阶次截面带宽"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", AnalysisStepParameterCatalog.FormatChoices()),
        Choice("window", "窗函数", "Hanning", AnalysisStepParameterCatalog.WindowChoices()),
        Choice("weight", "计权", "A", AnalysisStepParameterCatalog.WeightChoices()),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> RpmFrequencyMap() =>
    [
        ..AnalysisStepParameterCatalog.SharedRpmChannelParams(),
        ..AnalysisStepParameterCatalog.SharedRpmAxisParams(),
        ..AnalysisStepParameterCatalog.SharedMapSpectralParams()
    ];

    private static IReadOnlyList<AnalysisParameterDefinition> RpmOrderMap() =>
    [
        ..AnalysisStepParameterCatalog.SharedRpmChannelParams(),
        ..AnalysisStepParameterCatalog.SharedRpmAxisParams(),
        Double("maxOrder", "最大阶次", 32.0, "阶次图最大阶次"),
        Double("orderResolution", "阶次分辨率", 0.25, "阶次轴分辨率"),
        Scientific("referenceValue", "参考值 (Pa)", 2.0e-5, "dB 换算参考声压"),
        Choice("format", "幅值格式", "Rms", AnalysisStepParameterCatalog.FormatChoices()),
        Choice("window", "窗函数", "Hanning", AnalysisStepParameterCatalog.WindowChoices()),
        Choice("weight", "计权", "A", AnalysisStepParameterCatalog.WeightChoices()),
        Choice("scale", "刻度", "Db", AnalysisStepParameterCatalog.ScaleChoices())
    ];

    private static AnalysisParameterDefinition Int(string key, string name, int value, string? description = null) =>
        AnalysisStepParameterCatalog.Int(key, name, value, description);

    private static AnalysisParameterDefinition Double(string key, string name, double value, string? description = null) =>
        AnalysisStepParameterCatalog.Double(key, name, value, description);

    private static AnalysisParameterDefinition Scientific(string key, string name, double value, string? description = null) =>
        AnalysisStepParameterCatalog.Scientific(key, name, value, description);

    private static AnalysisParameterDefinition Choice(
        string key,
        string name,
        string defaultValue,
        IReadOnlyList<AnalysisChoiceOption> choices,
        string? description = null) =>
        AnalysisStepParameterCatalog.Choice(key, name, defaultValue, choices, description);
}
