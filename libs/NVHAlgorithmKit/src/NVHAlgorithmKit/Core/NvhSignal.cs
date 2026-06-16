namespace NVHAlgorithmKit.Core;

/// <summary>
/// 带采样率的 NVH 时域信号容器，封装采样数据与元信息。
/// </summary>
public sealed class NvhSignal
{
    /// <summary>
    /// 初始化 NVH 信号实例。
    /// </summary>
    /// <param name="samples">时域采样序列，单位取决于物理量（如声压 Pa、加速度 m/s²、速度 mm/s）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="samples"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="samples"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public NvhSignal(double[] samples, double sampleRate)
    {
        SignalGuard.NotEmpty(samples);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        Samples = samples;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// 时域采样数据数组，按时间顺序排列。
    /// </summary>
    public double[] Samples { get; }

    /// <summary>
    /// 采样率，单位 Hz。
    /// </summary>
    public double SampleRate { get; }

    /// <summary>
    /// 采样点数量，等于 <see cref="Samples"/> 数组长度。
    /// </summary>
    public int Length => Samples.Length;

    /// <summary>
    /// 信号持续时间，单位秒，等于 <see cref="Length"/> / <see cref="SampleRate"/>。
    /// </summary>
    public double Duration => Samples.Length / SampleRate;

    /// <summary>
    /// 以新的采样数据创建信号副本，保持原采样率不变。
    /// </summary>
    /// <param name="samples">新的时域采样序列，须非空。</param>
    /// <returns>使用新采样数据、相同采样率的新 <see cref="NvhSignal"/> 实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="samples"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="samples"/> 为空数组时抛出。</exception>
    public NvhSignal WithSamples(double[] samples) => new(samples, SampleRate);
}
