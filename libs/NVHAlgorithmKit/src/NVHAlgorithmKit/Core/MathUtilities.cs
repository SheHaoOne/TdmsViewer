namespace NVHAlgorithmKit.Core;

/// <summary>
/// 通用数学工具，提供分贝换算、去趋势及数值辅助函数。
/// </summary>
public static class MathUtilities
{
    /// <summary>
    /// 声学标准参考声压，20 μPa（IEC 61672-1）。
    /// </summary>
    public const double DefaultReferencePressure = 20e-6;

    /// <summary>
    /// 返回大于等于指定值的最小 2 的幂次方，用于 FFT 补零长度计算。
    /// </summary>
    /// <param name="value">输入正整数，通常为原始信号采样点数。</param>
    /// <returns>大于等于 <paramref name="value"/> 的最小 2 的幂（如 1000 → 1024）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于等于 0 时抛出。</exception>
    public static int NextPowerOfTwo(int value)
    {
        SignalGuard.Positive(value, nameof(value));
        var power = 1;
        while (power < value)
        {
            power <<= 1;
        }

        return power;
    }

    /// <summary>
    /// 判断整数是否为 2 的幂次方。
    /// </summary>
    /// <param name="value">待判断的正整数。</param>
    /// <returns>若 <paramref name="value"/> 为 2 的幂则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// 将线性幅值转换为分贝（dB），公式：20·log₁₀(linear / reference)。
    /// </summary>
    /// <param name="linear">线性幅值，须大于 0；若为 0 或负数则返回负无穷。</param>
    /// <param name="reference">参考值，默认为 1.0；声学场景可使用 <see cref="DefaultReferencePressure"/>。</param>
    /// <returns>对应的分贝值（dB）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="reference"/> 小于等于 0 时抛出。</exception>
    public static double ToDecibels(double linear, double reference = 1.0)
    {
        SignalGuard.Positive(reference, nameof(reference));
        if (linear <= 0)
        {
            return double.NegativeInfinity;
        }

        return 20.0 * Math.Log10(linear / reference);
    }

    /// <summary>
    /// 将分贝值还原为线性幅值，公式：reference · 10^(dB/20)。
    /// </summary>
    /// <param name="decibels">分贝值（dB）。</param>
    /// <param name="reference">参考值，默认为 1.0。</param>
    /// <returns>对应的线性幅值。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="reference"/> 小于等于 0 时抛出。</exception>
    public static double FromDecibels(double decibels, double reference = 1.0)
    {
        SignalGuard.Positive(reference, nameof(reference));
        return reference * Math.Pow(10.0, decibels / 20.0);
    }

    /// <summary>
    /// 将信号归一化到 [-1, 1] 区间，以绝对值最大样本为基准。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>归一化后的新数组；若输入全为零则返回原数组副本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] Normalize(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var max = data.Max(Math.Abs);
        if (max == 0)
        {
            return (double[])data.Clone();
        }

        var normalized = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            normalized[i] = data[i] / max;
        }

        return normalized;
    }

    /// <summary>
    /// 去除信号直流分量（减去算术平均值）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>去均值后的新数组，均值为 0。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] Detrend(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var mean = data.Average();
        var result = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = data[i] - mean;
        }

        return result;
    }

    /// <summary>
    /// 去除信号线性趋势（最小二乘拟合直线后减去）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>去除线性漂移后的新数组，适用于积分前预处理。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] RemoveLinearTrend(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var n = data.Length;
        if (n == 1)
        {
            return new[] { 0.0 };
        }

        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;
        for (var i = 0; i < n; i++)
        {
            sumX += i;
            sumY += data[i];
            sumXY += i * data[i];
            sumX2 += i * i;
        }

        var denominator = n * sumX2 - sumX * sumX;
        var slope = denominator == 0 ? 0 : (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;

        var result = new double[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = data[i] - (intercept + slope * i);
        }

        return result;
    }
}
