using System.Numerics;
using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Transform;

/// <summary>
/// 快速傅里叶变换（Cooley-Tukey 基-2），支持实数与复数输入。
/// </summary>
public static class FourierTransform
{
    /// <summary>
    /// 对实数时域信号执行正向 FFT，不足 2 的幂时自动末尾补零。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>复数频谱数组，长度为大于等于 <paramref name="data"/> 长度的最小 2 的幂。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static Complex[] Forward(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var padded = PadToPowerOfTwo(data);
        var spectrum = new Complex[padded.Length];
        for (var i = 0; i < data.Length; i++)
        {
            spectrum[i] = new Complex(data[i], 0);
        }

        TransformInPlace(spectrum, inverse: false);
        return spectrum;
    }

    /// <summary>
    /// 对复数序列执行正向 FFT，不足 2 的幂时自动末尾补零。
    /// </summary>
    /// <param name="data">输入复数时域序列。</param>
    /// <returns>复数频谱数组，长度为大于等于输入长度的最小 2 的幂。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static Complex[] Forward(Complex[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length == 0)
        {
            throw new ArgumentException("Input must not be empty.", nameof(data));
        }

        var working = PadToPowerOfTwo(data);
        TransformInPlace(working, inverse: false);
        return working;
    }

    /// <summary>
    /// 对频谱执行逆 FFT，还原时域实部信号。
    /// </summary>
    /// <param name="spectrum">输入复数频谱，长度须为 2 的幂。</param>
    /// <returns>还原的时域实部数组，长度与 <paramref name="spectrum"/> 相同。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="spectrum"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="spectrum"/> 为空或长度不是 2 的幂时抛出。</exception>
    public static double[] Inverse(Complex[] spectrum)
    {
        if (spectrum is null)
        {
            throw new ArgumentNullException(nameof(spectrum));
        }
        if (spectrum.Length == 0)
        {
            throw new ArgumentException("Spectrum must not be empty.", nameof(spectrum));
        }

        if (!MathUtilities.IsPowerOfTwo(spectrum.Length))
        {
            throw new ArgumentException("Spectrum length must be a power of two.", nameof(spectrum));
        }

        var working = (Complex[])spectrum.Clone();
        TransformInPlace(working, inverse: true);
        var result = new double[spectrum.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = working[i].Real;
        }

        return result;
    }

    /// <summary>
    /// 原地执行 FFT 或逆 FFT，直接修改输入数组。
    /// </summary>
    /// <param name="data">复数数组，长度须为 2 的幂；计算完成后被原地覆盖。</param>
    /// <param name="inverse">
    /// 变换方向：<see langword="false"/> 为正向 FFT（时域→频域），
    /// <see langword="true"/> 为逆 FFT（频域→时域，结果除以 N）。
    /// </param>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 长度不是 2 的幂时抛出。</exception>
    public static void TransformInPlace(Complex[] data, bool inverse)
    {
        var n = data.Length;
        if (!MathUtilities.IsPowerOfTwo(n))
        {
            throw new ArgumentException("Length must be a power of two.", nameof(data));
        }

        BitReversePermute(data);

        for (var size = 2; size <= n; size <<= 1)
        {
            var halfSize = size / 2;
            var angle = (inverse ? 2.0 : -2.0) * Math.PI / size;
            var wStep = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (var i = 0; i < n; i += size)
            {
                var w = Complex.One;
                for (var j = 0; j < halfSize; j++)
                {
                    var even = data[i + j];
                    var odd = data[i + j + halfSize] * w;
                    data[i + j] = even + odd;
                    data[i + j + halfSize] = even - odd;
                    w *= wStep;
                }
            }
        }

        if (inverse)
        {
            for (var i = 0; i < n; i++)
            {
                data[i] /= n;
            }
        }
    }

    /// <summary>
    /// 计算实数信号单边频谱幅值（仅返回 0 ~ Nyquist 频率部分）。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>幅值数组，长度为 FFT 长度 / 2 + 1。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] RealForwardMagnitude(double[] data)
    {
        var spectrum = Forward(data);
        var half = spectrum.Length / 2;
        var magnitudes = new double[half + 1];
        for (var i = 0; i <= half; i++)
        {
            magnitudes[i] = spectrum[i].Magnitude;
        }

        return magnitudes;
    }

    /// <summary>
    /// 生成 FFT 对应的频率轴（0 ~ Nyquist）。
    /// </summary>
    /// <param name="fftLength">FFT 长度（采样点数），须为 2 的幂且大于 0。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <returns>频率轴数组，长度 fftLength/2 + 1，单位 Hz，步长为 sampleRate/fftLength。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="fftLength"/> 或 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double[] FrequencyAxis(int fftLength, double sampleRate)
    {
        SignalGuard.Positive(fftLength, nameof(fftLength));
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        var half = fftLength / 2;
        var axis = new double[half + 1];
        for (var i = 0; i <= half; i++)
        {
            axis[i] = i * sampleRate / fftLength;
        }

        return axis;
    }

    private static double[] PadToPowerOfTwo(double[] data)
    {
        var targetLength = MathUtilities.NextPowerOfTwo(data.Length);
        if (targetLength == data.Length)
        {
            return (double[])data.Clone();
        }

        var padded = new double[targetLength];
        Array.Copy(data, padded, data.Length);
        return padded;
    }

    private static Complex[] PadToPowerOfTwo(Complex[] data)
    {
        if (MathUtilities.IsPowerOfTwo(data.Length))
        {
            return (Complex[])data.Clone();
        }

        var targetLength = MathUtilities.NextPowerOfTwo(data.Length);
        var padded = new Complex[targetLength];
        Array.Copy(data, padded, data.Length);
        return padded;
    }

    private static void BitReversePermute(Complex[] data)
    {
        var n = data.Length;
        var bits = (int)(Math.Log(n) / Math.Log(2));
        for (var i = 0; i < n; i++)
        {
            var j = ReverseBits(i, bits);
            if (j > i)
            {
                (data[i], data[j]) = (data[j], data[i]);
            }
        }
    }

    private static int ReverseBits(int value, int bits)
    {
        var result = 0;
        for (var i = 0; i < bits; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }
}
