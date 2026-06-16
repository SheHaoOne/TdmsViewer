using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Acoustics;

/// <summary>
/// Zwicker 响度分析结果。
/// </summary>
public sealed class LoudnessResult
{
    /// <summary>总响度，单位 sone。</summary>
    public double TotalLoudness { get; init; }

    /// <summary>总响度级，单位 phon（对应 1 kHz 参考）。</summary>
    public double LoudnessLevelPhon { get; init; }

    /// <summary>各 1/3 倍频程中心频率，单位 Hz。</summary>
    public double[] BandFrequencies { get; init; } = Array.Empty<double>();

    /// <summary>各频带特征响度（specific loudness），单位 sone/Bark。</summary>
    public double[] SpecificLoudness { get; init; } = Array.Empty<double>();
}

/// <summary>
/// Zwicker 响度分析器（ISO 532-1 简化模型），将声压转换为 sone 响度单位。
/// </summary>
public static class LoudnessAnalyzer
{
    /// <summary>
    /// 计算 Zwicker 总响度。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>总响度，单位 sone；静音时返回 0。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static double ComputeLoudness(double[] pressure, double sampleRate)
    {
        return ComputeDetailed(pressure, sampleRate).TotalLoudness;
    }

    /// <summary>
    /// 计算详细响度结果，包含特征响度分布。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>响度分析结果 <see cref="LoudnessResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static LoudnessResult ComputeDetailed(double[] pressure, double sampleRate)
    {
        SignalGuard.NotEmpty(pressure);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var octave = OctaveBandAnalyzer.Analyze(pressure, sampleRate, OctaveFraction.Third, WeightingType.Z);
        var overallSpl = AcousticWeighting.ComputeWeightedLevel(pressure, sampleRate, WeightingType.Z);
        var maxBandLevel = octave.Levels
            .Where(l => !double.IsNegativeInfinity(l))
            .DefaultIfEmpty(double.NegativeInfinity)
            .Max();
        var levelOffset = double.IsNegativeInfinity(maxBandLevel) ? 0 : overallSpl - maxBandLevel;

        var specificLoudness = new double[octave.Levels.Length];
        for (var i = 0; i < octave.Levels.Length; i++)
        {
            var levelDb = octave.Levels[i];
            if (double.IsNegativeInfinity(levelDb))
            {
                specificLoudness[i] = 0;
                continue;
            }

            specificLoudness[i] = BandLoudnessFromLevel(levelDb + levelOffset);
        }

        ApplySpreading(specificLoudness);
        var total = specificLoudness.Sum();
        var phon = total > 0 ? 40.0 + 10.0 * Math.Log10(total) : 0;

        return new LoudnessResult
        {
            TotalLoudness = total,
            LoudnessLevelPhon = phon,
            BandFrequencies = octave.CenterFrequencies,
            SpecificLoudness = specificLoudness
        };
    }

    private static double BandLoudnessFromLevel(double levelDb)
    {
        if (levelDb < 0 || double.IsNegativeInfinity(levelDb))
        {
            return 0;
        }

        if (levelDb < 40)
        {
            return Math.Pow(10, (levelDb - 40) / 10.0) * 0.002;
        }

        return Math.Pow(10, (levelDb - 40) / 10.0);
    }

    private static void ApplySpreading(double[] specificLoudness)
    {
        var spread = new double[specificLoudness.Length];
        for (var i = 0; i < specificLoudness.Length; i++)
        {
            for (var j = 0; j < specificLoudness.Length; j++)
            {
                var distance = Math.Abs(i - j);
                var factor = distance == 0 ? 1.0 : Math.Exp(-0.5 * distance);
                spread[i] += specificLoudness[j] * factor * 0.15;
            }
        }

        for (var i = 0; i < specificLoudness.Length; i++)
        {
            specificLoudness[i] += spread[i];
        }
    }
}
