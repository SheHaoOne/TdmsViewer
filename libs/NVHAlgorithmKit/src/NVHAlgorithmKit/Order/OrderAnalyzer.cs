using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Order;

/// <summary>
/// 阶次谱分析结果。
/// </summary>
public sealed class OrderSpectrumResult
{
    /// <summary>阶次轴数组，无量纲（如 1 阶 = 转频，2 阶 = 2 倍转频）。</summary>
    public double[] Orders { get; init; } = Array.Empty<double>();

    /// <summary>各阶次对应的幅值（归一化能量）。</summary>
    public double[] Magnitude { get; init; } = Array.Empty<double>();

    /// <summary>分析过程中使用的平均转速，单位 RPM（转/分钟）。</summary>
    public double ReferenceRpm { get; init; }
}

/// <summary>
/// 旋转机械阶次分析器，将频率谱映射到阶次域以消除转速波动影响。
/// </summary>
public static class OrderAnalyzer
{
    /// <summary>
    /// 计算阶次谱，适用于转速变化或稳态旋转机械振动分析。
    /// </summary>
    /// <param name="signal">振动时域序列（加速度或速度），长度须与 <paramref name="rpmTrace"/> 相同。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="rpmTrace">与信号同步的转速轨迹，单位 RPM，逐采样点对应。</param>
    /// <param name="orderCount">阶次分辨率（阶次轴点数），默认 64，须大于 0。</param>
    /// <param name="maxOrder">最大分析阶次，默认 10.0（即分析到 10 倍转频），须大于 0。</param>
    /// <returns>阶次谱结果 <see cref="OrderSpectrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 或 <paramref name="rpmTrace"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="signal"/> 为空，或与 <paramref name="rpmTrace"/> 长度不一致时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/>、<paramref name="orderCount"/> 或 <paramref name="maxOrder"/> 无效时抛出。</exception>
    public static OrderSpectrumResult ComputeOrderSpectrum(
        double[] signal,
        double sampleRate,
        double[] rpmTrace,
        int orderCount = 64,
        double maxOrder = 10.0)
    {
        SignalGuard.NotEmpty(signal);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.NotNull(rpmTrace);
        if (rpmTrace.Length != signal.Length)
        {
            throw new ArgumentException("RPM trace must match signal length.");
        }

        SignalGuard.Positive(orderCount, nameof(orderCount));
        SignalGuard.Positive(maxOrder, nameof(maxOrder));

        var orders = new double[orderCount];
        var magnitude = new double[orderCount];
        var orderStep = maxOrder / (orderCount - 1);
        for (var i = 0; i < orderCount; i++)
        {
            orders[i] = i * orderStep;
        }

        var segmentLength = Math.Min(4096, MathUtilities.NextPowerOfTwo(signal.Length / 4));
        segmentLength = Math.Max(256, segmentLength);
        var hop = segmentLength / 2;

        for (var start = 0; start + segmentLength <= signal.Length; start += hop)
        {
            var segment = new double[segmentLength];
            Array.Copy(signal, start, segment, 0, segmentLength);
            var meanRpm = 0.0;
            for (var i = start; i < start + segmentLength; i++)
            {
                meanRpm += rpmTrace[i];
            }

            meanRpm /= segmentLength;
            if (meanRpm <= 0)
            {
                continue;
            }

            var fundamental = meanRpm / 60.0;
            var spectrum = FrequencyDomain.SpectrumAnalyzer.Analyze(segment, sampleRate);
            for (var i = 0; i < orderCount; i++)
            {
                var targetFrequency = orders[i] * fundamental;
                var binEnergy = InterpolateSpectrum(spectrum.Frequencies, spectrum.Magnitude, targetFrequency);
                magnitude[i] += binEnergy;
            }
        }

        var validSegments = Math.Max(1, (signal.Length - segmentLength) / hop + 1);
        for (var i = 0; i < magnitude.Length; i++)
        {
            magnitude[i] /= validSegments;
        }

        return new OrderSpectrumResult
        {
            Orders = orders,
            Magnitude = magnitude,
            ReferenceRpm = rpmTrace.Average()
        };
    }

    /// <summary>
    /// 将转速与阶次转换为频率。
    /// </summary>
    /// <param name="rpm">转速，单位 RPM（转/分钟）。</param>
    /// <param name="order">阶次，默认 1.0（基频/转频）；2.0 表示 2 倍转频。</param>
    /// <returns>对应频率，单位 Hz，公式：rpm / 60 × order。</returns>
    public static double RpmToFrequency(double rpm, double order = 1.0)
    {
        return rpm / 60.0 * order;
    }

    /// <summary>
    /// 将频率与转速转换为阶次。
    /// </summary>
    /// <param name="frequency">频率，单位 Hz。</param>
    /// <param name="rpm">转速，单位 RPM，须大于 0。</param>
    /// <returns>阶次值，公式：frequency / (rpm / 60)。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="rpm"/> 小于等于 0 时抛出。</exception>
    public static double FrequencyToOrder(double frequency, double rpm)
    {
        if (rpm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rpm), rpm, "RPM must be positive.");
        }

        return frequency / (rpm / 60.0);
    }

    private static double InterpolateSpectrum(double[] frequencies, double[] magnitudes, double targetFrequency)
    {
        if (targetFrequency <= frequencies[0] || targetFrequency >= frequencies[frequencies.Length - 1])
        {
            return 0;
        }

        var index = Array.BinarySearch(frequencies, targetFrequency);
        if (index >= 0)
        {
            return magnitudes[index];
        }

        index = ~index;
        var x0 = frequencies[index - 1];
        var x1 = frequencies[index];
        var y0 = magnitudes[index - 1];
        var y1 = magnitudes[index];
        var ratio = (targetFrequency - x0) / (x1 - x0);
        return y0 + ratio * (y1 - y0);
    }
}
