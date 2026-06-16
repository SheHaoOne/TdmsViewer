using System.Numerics;
using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Transform;

/// <summary>
/// 连续小波变换（CWT）分析结果，适用于非平稳信号时频分析。
/// </summary>
public sealed class CwtResult
{
    /// <summary>分析频率轴，单位 Hz，从低到高排列。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>时间轴，单位秒。</summary>
    public double[] TimeAxis { get; init; } = Array.Empty<double>();

    /// <summary>小波系数幅值矩阵，第一维为频率索引，第二维为时间索引。</summary>
    public double[,] Magnitude { get; init; } = new double[0, 0];

    /// <summary>使用的 Morlet 小波中心频率参数 ω₀，默认 6.0。</summary>
    public double CenterFrequency { get; init; }
}

/// <summary>
/// 连续小波变换（Morlet 小波），适合瞬态与变频信号分析。
/// </summary>
public static class ContinuousWaveletTransform
{
    /// <summary>
    /// 对信号执行 Morlet 连续小波变换。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="minFrequency">最低分析频率，单位 Hz，默认 10 Hz。</param>
    /// <param name="maxFrequency">最高分析频率，单位 Hz，默认 Nyquist/4。</param>
    /// <param name="frequencyCount">频率点数（尺度数），默认 64，须大于 0。</param>
    /// <param name="centerFrequency">Morlet 小波中心频率参数 ω₀，默认 6.0，影响时频分辨率权衡。</param>
    /// <returns>CWT 时频分析结果 <see cref="CwtResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当频率或参数超出有效范围时抛出。</exception>
    public static CwtResult AnalyzeMorlet(
        double[] data,
        double sampleRate,
        double minFrequency = 10,
        double maxFrequency = 0,
        int frequencyCount = 64,
        double centerFrequency = 6.0)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(frequencyCount, nameof(frequencyCount));
        SignalGuard.Positive(minFrequency, nameof(minFrequency));

        if (maxFrequency <= 0)
        {
            maxFrequency = sampleRate / 4.0;
        }

        if (maxFrequency <= minFrequency)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFrequency), maxFrequency, "Max frequency must exceed min frequency.");
        }

        var n = data.Length;
        var frequencies = new double[frequencyCount];
        var logMin = Math.Log10(minFrequency);
        var logMax = Math.Log10(maxFrequency);
        for (var i = 0; i < frequencyCount; i++)
        {
            frequencies[i] = Math.Pow(10, logMin + i * (logMax - logMin) / (frequencyCount - 1));
        }

        var timeAxis = new double[n];
        for (var i = 0; i < n; i++)
        {
            timeAxis[i] = i / sampleRate;
        }

        var magnitude = new double[frequencyCount, n];
        var fftLength = MathUtilities.NextPowerOfTwo(n * 2);
        var signalFft = FourierTransform.Forward(data);
        var paddedLength = signalFft.Length;

        for (var fi = 0; fi < frequencyCount; fi++)
        {
            var freq = frequencies[fi];
            var scale = centerFrequency / (2.0 * Math.PI * freq);
            var waveletLength = Math.Min(n, Math.Max(16, (int)Math.Ceiling(scale * 10)));
            var wavelet = CreateMorletWavelet(waveletLength, scale, centerFrequency);
            var waveletFft = FourierTransform.Forward(PadWavelet(wavelet, paddedLength));

            for (var k = 0; k < paddedLength; k++)
            {
                waveletFft[k] = signalFft[k] * Complex.Conjugate(waveletFft[k]);
            }

            FourierTransform.TransformInPlace(waveletFft, inverse: true);
            for (var t = 0; t < n; t++)
            {
                magnitude[fi, t] = waveletFft[t].Magnitude / Math.Sqrt(scale);
            }
        }

        return new CwtResult
        {
            Frequencies = frequencies,
            TimeAxis = timeAxis,
            Magnitude = magnitude,
            CenterFrequency = centerFrequency
        };
    }

    private static double[] CreateMorletWavelet(int length, double scale, double omega0)
    {
        var wavelet = new double[length];
        var center = length / 2.0;
        var norm = Math.Pow(Math.PI, -0.25) / Math.Sqrt(scale);

        for (var i = 0; i < length; i++)
        {
            var t = (i - center) / scale;
            var envelope = Math.Exp(-0.5 * t * t);
            wavelet[i] = norm * envelope * Math.Cos(omega0 * t);
        }

        return wavelet;
    }

    private static double[] PadWavelet(double[] wavelet, int targetLength)
    {
        var padded = new double[targetLength];
        var offset = (targetLength - wavelet.Length) / 2;
        Array.Copy(wavelet, 0, padded, offset, wavelet.Length);
        return padded;
    }
}
