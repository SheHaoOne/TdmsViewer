using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.TimeDomain;

namespace NVHAlgorithmKit.Acoustics;

/// <summary>
/// 声品质指标集合（基于 Zwicker 模型简化实现）。
/// </summary>
public sealed class SoundQualityResult
{
    /// <summary>尖锐度（Sharpness），单位 acum，值越大声音越尖锐。</summary>
    public double Sharpness { get; init; }

    /// <summary>粗糙度（Roughness），单位 asper，值越大声音越粗糙。</summary>
    public double Roughness { get; init; }

    /// <summary>波动度（Fluctuation Strength），单位 vacil，值越大起伏感越强。</summary>
    public double FluctuationStrength { get; init; }

    /// <summary>总声压级，单位 dB(A)。</summary>
    public double LoudnessLevel { get; init; }
}

/// <summary>
/// 声品质指标计算器（ECMA-418 / DIN 45692 简化模型），用于主观噪声评价。
/// </summary>
public static class SoundQualityMetrics
{
    private static readonly double[] SharpnessWeights =
    {
        0.0, 0.05, 0.10, 0.15, 0.20, 0.30, 0.40, 0.50, 0.60, 0.70,
        0.80, 0.85, 0.90, 0.95, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00,
        1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00
    };

    /// <summary>
    /// 计算尖锐度（Sharpness），反映高频能量对主观尖锐感的影响。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>尖锐度，单位 acum，典型范围 0~3。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static double ComputeSharpness(double[] pressure, double sampleRate)
    {
        return ComputeAll(pressure, sampleRate).Sharpness;
    }

    /// <summary>
    /// 计算粗糙度（Roughness），反映 15~300 Hz 调制引起的粗糙感。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>粗糙度，单位 asper，典型范围 0~3。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static double ComputeRoughness(double[] pressure, double sampleRate)
    {
        return ComputeAll(pressure, sampleRate).Roughness;
    }

    /// <summary>
    /// 计算波动度（Fluctuation Strength），反映 ~4 Hz 调制引起的起伏感。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>波动度，单位 vacil，典型范围 0~2。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static double ComputeFluctuationStrength(double[] pressure, double sampleRate)
    {
        return ComputeAll(pressure, sampleRate).FluctuationStrength;
    }

    /// <summary>
    /// 一次性计算全部声品质指标。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>声品质指标集合 <see cref="SoundQualityResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    public static SoundQualityResult ComputeAll(double[] pressure, double sampleRate)
    {
        SignalGuard.NotEmpty(pressure);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var octave = OctaveBandAnalyzer.Analyze(pressure, sampleRate, OctaveFraction.Third, WeightingType.A);
        var loudness = OctaveBandAnalyzer.TotalLevel(octave);
        var sharpness = ComputeSharpnessFromBands(octave, loudness);
        var roughness = ComputeModulationMetric(pressure, sampleRate, modFrequency: 70, scale: 0.0003);
        var fluctuation = ComputeModulationMetric(pressure, sampleRate, modFrequency: 4, scale: 0.008);

        return new SoundQualityResult
        {
            Sharpness = sharpness,
            Roughness = roughness,
            FluctuationStrength = fluctuation,
            LoudnessLevel = loudness
        };
    }

    private static double ComputeSharpnessFromBands(OctaveBandResult octave, double totalLevelDb)
    {
        if (double.IsNegativeInfinity(totalLevelDb))
        {
            return 0;
        }

        var numerator = 0.0;
        var denominator = 0.0;
        for (var i = 0; i < octave.Levels.Length; i++)
        {
            var level = octave.Levels[i];
            if (double.IsNegativeInfinity(level))
            {
                continue;
            }

            var energy = Math.Pow(10, level / 10.0);
            var weight = i < SharpnessWeights.Length ? SharpnessWeights[i] : 1.0;
            numerator += weight * energy * octave.CenterFrequencies[i];
            denominator += energy;
        }

        if (denominator <= 0)
        {
            return 0;
        }

        var specificLevel = numerator / denominator;
        return 0.11 * specificLevel / 1000.0 * (totalLevelDb / 40.0 + 0.2);
    }

    private static double ComputeModulationMetric(double[] pressure, double sampleRate, double modFrequency, double scale)
    {
        var envelope = Vibration.EnvelopeAnalysis.ComputeEnvelope(pressure);
        envelope = MathUtilities.Detrend(envelope);

        var spectrum = FrequencyDomain.SpectrumAnalyzer.Analyze(envelope, sampleRate);
        var targetIndex = 0;
        var maxMag = 0.0;
        for (var i = 0; i < spectrum.Frequencies.Length; i++)
        {
            if (spectrum.Frequencies[i] > modFrequency * 0.5 && spectrum.Frequencies[i] < modFrequency * 2)
            {
                if (spectrum.Magnitude[i] > maxMag)
                {
                    maxMag = spectrum.Magnitude[i];
                    targetIndex = i;
                }
            }
        }

        var rms = TimeDomainMetrics.Rms(envelope);
        if (rms <= 0)
        {
            return 0;
        }

        return scale * spectrum.Magnitude[targetIndex] / rms * 1000;
    }
}
