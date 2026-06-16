namespace NVHAlgorithmKit.Core;

/// <summary>
/// 数字信号重采样器，支持任意采样率之间的转换。
/// </summary>
public static class SignalResampler
{
    /// <summary>
    /// 将信号重采样到目标采样率（线性插值）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="sourceSampleRate">原始采样率，单位 Hz，须大于 0。</param>
    /// <param name="targetSampleRate">目标采样率，单位 Hz，须大于 0。</param>
    /// <returns>重采样后的时域序列。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当采样率无效时抛出。</exception>
    public static double[] Resample(double[] data, double sourceSampleRate, double targetSampleRate)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sourceSampleRate, nameof(sourceSampleRate));
        SignalGuard.Positive(targetSampleRate, nameof(targetSampleRate));

        if (Math.Abs(sourceSampleRate - targetSampleRate) < 1e-9)
        {
            return (double[])data.Clone();
        }

        var ratio = sourceSampleRate / targetSampleRate;
        var outputLength = (int)Math.Max(1, Math.Round(data.Length / ratio));
        var result = new double[outputLength];

        for (var i = 0; i < outputLength; i++)
        {
            var sourceIndex = i * ratio;
            var index0 = (int)Math.Floor(sourceIndex);
            var index1 = Math.Min(index0 + 1, data.Length - 1);
            var fraction = sourceIndex - index0;
            result[i] = data[index0] * (1.0 - fraction) + data[index1] * fraction;
        }

        return result;
    }

    /// <summary>
    /// 将 <see cref="NvhSignal"/> 重采样到目标采样率。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="targetSampleRate">目标采样率，单位 Hz，须大于 0。</param>
    /// <returns>重采样后的新 <see cref="NvhSignal"/> 实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="targetSampleRate"/> 小于等于 0 时抛出。</exception>
    public static NvhSignal Resample(NvhSignal signal, double targetSampleRate)
    {
        if (signal is null)
        {
            throw new ArgumentNullException(nameof(signal));
        }
        var resampled = Resample(signal.Samples, signal.SampleRate, targetSampleRate);
        return new NvhSignal(resampled, targetSampleRate);
    }

    /// <summary>
    /// 按整数倍降采样（抽取，无抗混叠滤波，适用于已滤波信号）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="factor">降采样因子，须大于等于 2。</param>
    /// <returns>降采样后的序列，长度为原长度 / factor。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="factor"/> 小于 2 时抛出。</exception>
    public static double[] Decimate(double[] data, int factor)
    {
        SignalGuard.NotEmpty(data);
        if (factor < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "Decimation factor must be at least 2.");
        }

        var outputLength = data.Length / factor;
        var result = new double[outputLength];
        for (var i = 0; i < outputLength; i++)
        {
            result[i] = data[i * factor];
        }

        return result;
    }
}
