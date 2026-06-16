using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.TimeDomain;

/// <summary>
/// 时域统计与振动特征指标计算。
/// </summary>
public static class TimeDomainMetrics
{
    /// <summary>
    /// 计算均方根值（RMS）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>RMS 值，公式 √(Σx²/N)。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double Rms(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var sum = 0.0;
        foreach (var sample in data)
        {
            sum += sample * sample;
        }

        return Math.Sqrt(sum / data.Length);
    }

    /// <summary>
    /// 计算峰值（绝对值最大值）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>峰值，即 max(|xᵢ|)。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double Peak(double[] data)
    {
        SignalGuard.NotEmpty(data);
        return data.Max(Math.Abs);
    }

    /// <summary>
    /// 计算峰峰值（最大值与最小值之差）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>峰峰值，即 max(x) - min(x)。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double PeakToPeak(double[] data)
    {
        SignalGuard.NotEmpty(data);
        return data.Max() - data.Min();
    }

    /// <summary>
    /// 计算算术平均值。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>算术均值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double Mean(double[] data)
    {
        SignalGuard.NotEmpty(data);
        return data.Average();
    }

    /// <summary>
    /// 计算波峰因子（峰值 / RMS），反映信号冲击程度。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>波峰因子；RMS 为 0 时返回 0。正常振动约 1.414（正弦波），冲击信号可达 3~5。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double CrestFactor(double[] data)
    {
        var rms = Rms(data);
        return rms == 0 ? 0 : Peak(data) / rms;
    }

    /// <summary>
    /// 计算偏度，衡量分布不对称性。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>偏度值；正态分布约为 0，右偏为正，左偏为负。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double Skewness(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var mean = Mean(data);
        var n = data.Length;
        var m2 = 0.0;
        var m3 = 0.0;
        foreach (var sample in data)
        {
            var delta = sample - mean;
            m2 += delta * delta;
            m3 += delta * delta * delta;
        }

        m2 /= n;
        m3 /= n;
        if (m2 == 0)
        {
            return 0;
        }

        return m3 / Math.Pow(m2, 1.5);
    }

    /// <summary>
    /// 计算峭度，衡量分布尖锐程度（正态分布峭度为 3，此处返回原始峭度非超额峭度）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>峭度值；大于 3 表示存在冲击成分。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double Kurtosis(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var mean = Mean(data);
        var n = data.Length;
        var m2 = 0.0;
        var m4 = 0.0;
        foreach (var sample in data)
        {
            var delta = sample - mean;
            var delta2 = delta * delta;
            m2 += delta2;
            m4 += delta2 * delta2;
        }

        m2 /= n;
        m4 /= n;
        if (m2 == 0)
        {
            return 0;
        }

        return m4 / (m2 * m2);
    }

    /// <summary>
    /// 计算裕度因子，用于轴承故障早期检测。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>裕度因子，公式 Peak / (mean(√|x|))²。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double ClearanceFactor(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var peak = Peak(data);
        var meanRoot = 0.0;
        foreach (var sample in data)
        {
            meanRoot += Math.Sqrt(Math.Abs(sample));
        }

        meanRoot /= data.Length;
        if (meanRoot == 0)
        {
            return 0;
        }

        return peak / (meanRoot * meanRoot);
    }

    /// <summary>
    /// 计算脉冲因子，反映信号中冲击成分占比。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>脉冲因子，公式 Peak / mean(|x|)。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double ImpulseFactor(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var peak = Peak(data);
        var meanAbs = data.Average(Math.Abs);
        return meanAbs == 0 ? 0 : peak / meanAbs;
    }

    /// <summary>
    /// 计算波形因子，反映波形偏离正弦的程度。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>波形因子，公式 RMS / mean(|x|)；正弦波约为 1.111。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double ShapeFactor(double[] data)
    {
        var rms = Rms(data);
        var meanAbs = data.Average(Math.Abs);
        return meanAbs == 0 ? 0 : rms / meanAbs;
    }

    /// <summary>
    /// 一次性计算全部时域特征指标。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>包含所有时域特征的 <see cref="TimeDomainFeatures"/> 对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static TimeDomainFeatures ComputeAll(double[] data)
    {
        return new TimeDomainFeatures
        {
            Mean = Mean(data),
            Rms = Rms(data),
            Peak = Peak(data),
            PeakToPeak = PeakToPeak(data),
            CrestFactor = CrestFactor(data),
            Skewness = Skewness(data),
            Kurtosis = Kurtosis(data),
            ClearanceFactor = ClearanceFactor(data),
            ImpulseFactor = ImpulseFactor(data),
            ShapeFactor = ShapeFactor(data)
        };
    }
}

/// <summary>
/// 时域特征指标集合。
/// </summary>
public sealed class TimeDomainFeatures
{
    /// <summary>算术平均值。</summary>
    public double Mean { get; init; }

    /// <summary>均方根值（RMS）。</summary>
    public double Rms { get; init; }

    /// <summary>峰值（绝对值最大）。</summary>
    public double Peak { get; init; }

    /// <summary>峰峰值。</summary>
    public double PeakToPeak { get; init; }

    /// <summary>波峰因子（Peak / RMS）。</summary>
    public double CrestFactor { get; init; }

    /// <summary>偏度。</summary>
    public double Skewness { get; init; }

    /// <summary>峭度。</summary>
    public double Kurtosis { get; init; }

    /// <summary>裕度因子。</summary>
    public double ClearanceFactor { get; init; }

    /// <summary>脉冲因子。</summary>
    public double ImpulseFactor { get; init; }

    /// <summary>波形因子。</summary>
    public double ShapeFactor { get; init; }
}
