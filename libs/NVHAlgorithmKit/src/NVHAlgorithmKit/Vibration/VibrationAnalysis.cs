using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Filtering;
using NVHAlgorithmKit.Transform;

namespace NVHAlgorithmKit.Vibration;

/// <summary>
/// 振动量时域积分器，实现加速度、速度、位移之间的转换。
/// </summary>
public static class VibrationIntegrator
{
    /// <summary>
    /// 对加速度信号积分一次得到速度信号。
    /// </summary>
    /// <param name="acceleration">加速度时域序列，单位 m/s²（或 g，结果单位相应变化）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="highPassCutoff">高通截止频率，单位 Hz，用于抑制积分漂移，默认 0.5 Hz。</param>
    /// <returns>速度时域序列，单位 m/s（与输入加速度单位对应）。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="acceleration"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="acceleration"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double[] Integrate(double[] acceleration, double sampleRate, double highPassCutoff = 0.5)
    {
        SignalGuard.NotEmpty(acceleration);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var detrended = MathUtilities.RemoveLinearTrend(acceleration);
        var highPass = ButterworthDesigner.CreateHighPass(2, sampleRate, highPassCutoff);
        var filtered = highPass.ProcessBlock(detrended);

        var dt = 1.0 / sampleRate;
        var velocity = new double[filtered.Length];
        var cumulative = 0.0;
        for (var i = 0; i < filtered.Length; i++)
        {
            cumulative += filtered[i] * dt;
            velocity[i] = cumulative;
        }

        return MathUtilities.RemoveLinearTrend(velocity);
    }

    /// <summary>
    /// 对加速度信号积分两次得到位移信号。
    /// </summary>
    /// <param name="acceleration">加速度时域序列，单位 m/s²。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="highPassCutoff">高通截止频率，单位 Hz，默认 0.5 Hz，用于抑制积分漂移。</param>
    /// <returns>位移时域序列，单位 m。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="acceleration"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="acceleration"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double[] IntegrateTwice(double[] acceleration, double sampleRate, double highPassCutoff = 0.5)
    {
        var velocity = Integrate(acceleration, sampleRate, highPassCutoff);
        return Integrate(velocity, sampleRate, highPassCutoff);
    }

    /// <summary>
    /// 对信号进行数值微分（一阶差分）。
    /// </summary>
    /// <param name="signal">输入时域序列（如位移或速度）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>微分后的时域序列，单位取决于输入物理量与采样间隔。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="signal"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double[] Differentiate(double[] signal, double sampleRate)
    {
        SignalGuard.NotEmpty(signal);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var result = new double[signal.Length];
        if (signal.Length == 1)
        {
            return result;
        }

        var dt = 1.0 / sampleRate;
        for (var i = 1; i < signal.Length; i++)
        {
            result[i] = (signal[i] - signal[i - 1]) / dt;
        }

        result[0] = result[1];
        return result;
    }
}

/// <summary>
/// 包络分析器，基于 Hilbert 变换提取信号包络，用于轴承/齿轮故障诊断。
/// </summary>
public static class EnvelopeAnalysis
{
    /// <summary>
    /// 计算信号的包络（Hilbert 变换幅值）。
    /// </summary>
    /// <param name="data">输入时域序列，通常为共振频带滤波后的振动信号。</param>
    /// <returns>包络时域序列，长度与输入相同，全为非负值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] ComputeEnvelope(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var analytic = HilbertTransform(data);
        var envelope = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            envelope[i] = analytic[i].Magnitude;
        }

        return envelope;
    }

    /// <summary>
    /// 计算包络谱（对包络信号做 FFT 频谱分析）。
    /// </summary>
    /// <param name="data">输入时域序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>包络谱幅值数组，用于识别周期性冲击特征频率。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double[] ComputeEnvelopeSpectrum(double[] data, double sampleRate)
    {
        var envelope = ComputeEnvelope(data);
        var spectrum = FrequencyDomain.SpectrumAnalyzer.Analyze(envelope, sampleRate);
        return spectrum.Magnitude;
    }

    private static System.Numerics.Complex[] HilbertTransform(double[] data)
    {
        var spectrum = FourierTransform.Forward(data);
        var n = spectrum.Length;
        var half = n / 2;

        spectrum[0] = System.Numerics.Complex.Zero;
        if (n % 2 == 0)
        {
            spectrum[half] = System.Numerics.Complex.Zero;
        }

        for (var i = 1; i < half; i++)
        {
            spectrum[i] *= 2.0;
        }

        for (var i = half + 1; i < n; i++)
        {
            spectrum[i] = System.Numerics.Complex.Zero;
        }

        var timeDomain = FourierTransform.Inverse(spectrum);
        var analytic = new System.Numerics.Complex[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            analytic[i] = new System.Numerics.Complex(data[i], timeDomain[i]);
        }

        return analytic;
    }
}
