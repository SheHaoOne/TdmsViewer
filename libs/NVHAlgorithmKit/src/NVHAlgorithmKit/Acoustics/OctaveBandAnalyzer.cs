using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Acoustics;

/// <summary>
/// 倍频程分频比例。
/// </summary>
public enum OctaveFraction
{
    /// <summary>全倍频程（1/1），每个频带倍频（带宽为一个倍频程）。</summary>
    Full = 1,

    /// <summary>1/3 倍频程，每个频带为 1/3 倍频程宽，分辨率更高。</summary>
    Third = 3
}

/// <summary>
/// 倍频程分析结果。
/// </summary>
public sealed class OctaveBandResult
{
    /// <summary>各频带中心频率数组，单位 Hz（符合 IEC 61260 标准频率）。</summary>
    public double[] CenterFrequencies { get; init; } = Array.Empty<double>();

    /// <summary>各频带声压级数组，单位 dB；无能量时为负无穷。</summary>
    public double[] Levels { get; init; } = Array.Empty<double>();

    /// <summary>使用的倍频程分频比例。</summary>
    public OctaveFraction Fraction { get; init; }

    /// <summary>分析时使用的频率计权类型。</summary>
    public WeightingType Weighting { get; init; }
}

/// <summary>
/// IEC 61260 倍频程分析器，将频谱能量聚合到标准频带。
/// </summary>
public static class OctaveBandAnalyzer
{
    private static readonly double[] FullOctaveCenters =
    {
        31.5, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000
    };

    /// <summary>
    /// 对声压信号执行倍频程分析。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0；须至少为最高分析频率的 2 倍。</param>
    /// <param name="fraction">倍频程分频比例，默认 1/3 倍频程。</param>
    /// <param name="weighting">频率计权类型，默认 A 计权。</param>
    /// <returns>各频带的中心频率与声压级 <see cref="OctaveBandResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static OctaveBandResult Analyze(
        double[] pressure,
        double sampleRate,
        OctaveFraction fraction = OctaveFraction.Third,
        WeightingType weighting = WeightingType.A)
    {
        SignalGuard.NotEmpty(pressure);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var spectrum = FrequencyDomain.SpectrumAnalyzer.Analyze(pressure, sampleRate);
        var weightedPressure = weighting == WeightingType.Z
            ? pressure
            : AcousticWeighting.ApplyWeighting(pressure, sampleRate, weighting);

        var weightedSpectrum = FrequencyDomain.SpectrumAnalyzer.Analyze(weightedPressure, sampleRate);
        var centers = fraction == OctaveFraction.Full
            ? FullOctaveCenters.Where(f => f < sampleRate / 2).ToArray()
            : GenerateThirdOctaveCenters(sampleRate / 2);

        var levels = new double[centers.Length];
        for (var i = 0; i < centers.Length; i++)
        {
            var (low, high) = GetBandEdges(centers[i], fraction);
            var bandPower = 0.0;
            for (var j = 0; j < weightedSpectrum.Frequencies.Length; j++)
            {
                var frequency = weightedSpectrum.Frequencies[j];
                if (frequency >= low && frequency <= high)
                {
                    bandPower += weightedSpectrum.Power[j];
                }
            }

            levels[i] = bandPower <= 0 ? double.NegativeInfinity : 10.0 * Math.Log10(bandPower);
        }

        return new OctaveBandResult
        {
            CenterFrequencies = centers,
            Levels = levels,
            Fraction = fraction,
            Weighting = weighting
        };
    }

    /// <summary>
    /// 由倍频程各频带声压级计算总声压级（能量叠加）。
    /// </summary>
    /// <param name="result">倍频程分析结果，由 <see cref="Analyze"/> 生成。</param>
    /// <returns>总声压级，单位 dB；所有频带均无能量时返回负无穷。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="result"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double TotalLevel(OctaveBandResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }
        var powerSum = 0.0;
        foreach (var level in result.Levels)
        {
            if (!double.IsNegativeInfinity(level))
            {
                powerSum += Math.Pow(10.0, level / 10.0);
            }
        }

        return powerSum <= 0 ? double.NegativeInfinity : 10.0 * Math.Log10(powerSum);
    }

    private static double[] GenerateThirdOctaveCenters(double maxFrequency)
    {
        var centers = new List<double>();
        for (var band = -20; band <= 30; band++)
        {
            var center = 1000.0 * Math.Pow(10.0, band / 10.0);
            if (center >= 20 && center <= maxFrequency)
            {
                centers.Add(center);
            }
        }

        return centers.ToArray();
    }

    private static (double Low, double High) GetBandEdges(double centerFrequency, OctaveFraction fraction)
    {
        var factor = Math.Pow(2.0, 1.0 / (2.0 * (int)fraction));
        return (centerFrequency / factor, centerFrequency * factor);
    }
}
