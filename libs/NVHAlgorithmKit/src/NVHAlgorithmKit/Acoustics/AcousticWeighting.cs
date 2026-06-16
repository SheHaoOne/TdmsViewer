using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Filtering;

namespace NVHAlgorithmKit.Acoustics;

/// <summary>
/// 声学计权曲线类型（IEC 61672-1）。
/// </summary>
public enum WeightingType
{
    /// <summary>A 计权，模拟人耳对宽频噪声的感知，适用于环境噪声测量。</summary>
    A,

    /// <summary>C 计权，适用于峰值声压级及低频噪声测量。</summary>
    C,

    /// <summary>Z 计权（零计权/线性），不进行频率加权，平坦响应。</summary>
    Z
}

/// <summary>
/// 声学计权滤波与声压级（SPL）计算，符合 IEC 61672-1 标准。
/// </summary>
public static class AcousticWeighting
{
    /// <summary>
    /// 对声压时域信号施加频率计权。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa（帕斯卡）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="weighting">计权类型：A / C / Z。</param>
    /// <returns>计权后的声压时域序列，长度与输入相同；Z 计权返回原信号副本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 或 <paramref name="weighting"/> 无效时抛出。</exception>
    public static double[] ApplyWeighting(double[] pressure, double sampleRate, WeightingType weighting)
    {
        SignalGuard.NotEmpty(pressure);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        return weighting switch
        {
            WeightingType.Z => (double[])pressure.Clone(),
            WeightingType.A => ApplyAWeighting(pressure, sampleRate),
            WeightingType.C => ApplyCWeighting(pressure, sampleRate),
            _ => throw new ArgumentOutOfRangeException(nameof(weighting), weighting, "Unsupported weighting.")
        };
    }

    /// <summary>
    /// 计算声压级（SPL），不进行频率计权。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="referencePressure">参考声压，默认 20 μPa（<see cref="MathUtilities.DefaultReferencePressure"/>）。</param>
    /// <returns>声压级，单位 dB SPL。</returns>
    public static double ComputeLevel(double[] pressure, double referencePressure = MathUtilities.DefaultReferencePressure)
    {
        var rms = TimeDomain.TimeDomainMetrics.Rms(pressure);
        return MathUtilities.ToDecibels(rms, referencePressure);
    }

    /// <summary>
    /// 计算计权声压级（如 dB(A)）。
    /// </summary>
    /// <param name="pressure">声压时域序列，单位 Pa。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="weighting">计权类型：A / C / Z。</param>
    /// <returns>计权声压级，单位 dB（如 dB(A)、dB(C)、dB(Z)）。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="pressure"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="pressure"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 无效时抛出。</exception>
    public static double ComputeWeightedLevel(double[] pressure, double sampleRate, WeightingType weighting)
    {
        var weighted = ApplyWeighting(pressure, sampleRate, weighting);
        return ComputeLevel(weighted);
    }

    /// <summary>
    /// 获取各频率点的计权修正值（用于频域后处理）。
    /// </summary>
    /// <param name="frequencies">频率轴数组，单位 Hz，须非空。</param>
    /// <param name="weighting">计权类型：A / C / Z。</param>
    /// <returns>与 <paramref name="frequencies"/> 等长的修正值数组，单位 dB；Z 计权全为 0。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="frequencies"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double[] GetFrequencyCorrection(double[] frequencies, WeightingType weighting)
    {
        SignalGuard.NotNull(frequencies);
        var correction = new double[frequencies.Length];
        for (var i = 0; i < frequencies.Length; i++)
        {
            correction[i] = weighting switch
            {
                WeightingType.A => AWeightingDb(frequencies[i]),
                WeightingType.C => CWeightingDb(frequencies[i]),
                WeightingType.Z => 0,
                _ => 0
            };
        }

        return correction;
    }

    private static double[] ApplyAWeighting(double[] pressure, double sampleRate)
    {
        var filter = CreateAWeightingFilter(sampleRate);
        return filter.ProcessBlock(pressure);
    }

    private static double[] ApplyCWeighting(double[] pressure, double sampleRate)
    {
        var filter = CreateCWeightingFilter(sampleRate);
        return filter.ProcessBlock(pressure);
    }

    private static IFilter CreateAWeightingFilter(double sampleRate)
    {
        var highPass = new[]
        {
            new BiquadFilter(1.0, -2.0, 1.0, 1.0, -2.0 * Math.Cos(2.0 * Math.PI * 20.6 / sampleRate), 1.0),
            new BiquadFilter(1.0, -2.0, 1.0, 1.0, -2.0 * Math.Cos(2.0 * Math.PI * 20.6 / sampleRate), 1.0)
        };
        var highShelf = new BiquadFilter(1.0, 0.0, 0.0, 1.0, -2.0 * Math.Cos(2.0 * Math.PI * 107.7 / sampleRate), 1.0);
        var lowShelf = new BiquadFilter(1.0, -2.0, 1.0, 1.0, -2.0 * Math.Cos(2.0 * Math.PI * 737.9 / sampleRate), 1.0);
        var lowPass = new BiquadFilter(1.0, 2.0, 1.0, 1.0, 2.0 * Math.Cos(2.0 * Math.PI * 12200.0 / sampleRate), 1.0);

        return new CascadedFilter(new[] { highPass[0], highPass[1], highShelf, lowShelf, lowPass });
    }

    private static IFilter CreateCWeightingFilter(double sampleRate)
    {
        var highPass = new BiquadFilter(1.0, -2.0, 1.0, 1.0, -2.0 * Math.Cos(2.0 * Math.PI * 20.6 / sampleRate), 1.0);
        var lowPass = new BiquadFilter(1.0, 2.0, 1.0, 1.0, 2.0 * Math.Cos(2.0 * Math.PI * 12200.0 / sampleRate), 1.0);
        return new CascadedFilter(new[] { highPass, lowPass });
    }

    private static double AWeightingDb(double frequency)
    {
        if (frequency <= 0)
        {
            return double.NegativeInfinity;
        }

        var f2 = frequency * frequency;
        const double c1 = 12194.217 * 12194.217;
        const double c2 = 20.598997 * 20.598997;
        const double c3 = 107.65265 * 107.65265;
        const double c4 = 737.86223 * 737.86223;

        var numerator = c1 * f2 * f2;
        var denominator = (f2 + c2) * Math.Sqrt((f2 + c3) * (f2 + c4)) * (f2 + c1);
        return 20.0 * Math.Log10(numerator / denominator) + 2.0;
    }

    private static double CWeightingDb(double frequency)
    {
        if (frequency <= 0)
        {
            return double.NegativeInfinity;
        }

        var f2 = frequency * frequency;
        const double c1 = 12194.217 * 12194.217;
        const double c2 = 20.598997 * 20.598997;
        var numerator = c1 * f2;
        var denominator = (f2 + c2) * (f2 + c1);
        return 20.0 * Math.Log10(numerator / denominator);
    }
}
