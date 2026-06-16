using System.Numerics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// 倒谱分析结果，用于识别周期性冲击（齿轮/轴承故障）的回声模式。
/// </summary>
public sealed class CepstrumResult
{
    /// <summary>倒频率（quefrency）轴，单位秒。</summary>
    public double[] Quefrency { get; init; } = Array.Empty<double>();

    /// <summary>实倒谱幅值。</summary>
    public double[] RealCepstrum { get; init; } = Array.Empty<double>();

    /// <summary>倒频率分辨率，单位秒。</summary>
    public double QuefrencyResolution { get; init; }
}

/// <summary>
/// 倒谱分析器，通过二次频谱分析揭示信号中的周期性结构。
/// </summary>
public static class CepstrumAnalyzer
{
    /// <summary>
    /// 计算实倒谱：c[n] = IFFT(log(|FFT(x)|))。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="removeDc">是否去除直流分量，默认 <see langword="true"/>。</param>
    /// <returns>倒谱分析结果 <see cref="CepstrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static CepstrumResult ComputeRealCepstrum(double[] data, double sampleRate, bool removeDc = true)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var working = removeDc ? MathUtilities.Detrend(data) : (double[])data.Clone();
        var spectrum = FourierTransform.Forward(working);
        var logMagnitude = new Complex[spectrum.Length];

        for (var i = 0; i < spectrum.Length; i++)
        {
            var mag = spectrum[i].Magnitude;
            logMagnitude[i] = new Complex(Math.Log(Math.Max(mag, 1e-20)), 0);
        }

        var cepstrum = FourierTransform.Inverse(logMagnitude);
        var n = cepstrum.Length;
        var quefrency = new double[n];
        var realCepstrum = new double[n];

        for (var i = 0; i < n; i++)
        {
            quefrency[i] = i / sampleRate;
            realCepstrum[i] = cepstrum[i];
        }

        return new CepstrumResult
        {
            Quefrency = quefrency,
            RealCepstrum = realCepstrum,
            QuefrencyResolution = 1.0 / sampleRate
        };
    }

    /// <summary>
    /// 在倒谱中查找显著峰值对应的倒频率（周期性回声间隔）。
    /// </summary>
    /// <param name="result">倒谱分析结果。</param>
    /// <param name="minQuefrency">搜索下限倒频率，单位秒，默认 0.001（1 ms）。</param>
    /// <param name="maxQuefrency">搜索上限倒频率，单位秒，默认 1.0。</param>
    /// <returns>峰值倒频率，单位秒；可用于估算齿轮啮合周期或轴承故障周期。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="result"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double FindDominantQuefrency(CepstrumResult result, double minQuefrency = 0.001, double maxQuefrency = 1.0)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var maxValue = double.NegativeInfinity;
        var dominant = 0.0;

        for (var i = 0; i < result.Quefrency.Length; i++)
        {
            var q = result.Quefrency[i];
            if (q < minQuefrency || q > maxQuefrency)
            {
                continue;
            }

            if (result.RealCepstrum[i] > maxValue)
            {
                maxValue = result.RealCepstrum[i];
                dominant = q;
            }
        }

        return dominant;
    }
}
