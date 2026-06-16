using System.Numerics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;

namespace NVHAlgorithmKit.TimeDomain;

/// <summary>
/// 时域相关分析，包含自相关与互相关，用于时延估计与周期检测。
/// </summary>
public static class CorrelationAnalysis
{
    /// <summary>
    /// 计算两个信号的互相关函数。
    /// </summary>
    /// <param name="x">参考信号时域序列。</param>
    /// <param name="y">比较信号时域序列，长度须与 <paramref name="x"/> 相同。</param>
    /// <param name="maxLag">最大滞后点数，默认为信号长度 - 1；正滞后表示 y 相对 x 延迟。</param>
    /// <param name="normalize">是否归一化到 [-1, 1]，默认 <see langword="true"/>。</param>
    /// <returns>互相关数组，长度为 2 × maxLag + 1，零滞后位于中心索引 maxLag。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当两信号长度不一致或为空时抛出。</exception>
    public static double[] CrossCorrelation(double[] x, double[] y, int maxLag = -1, bool normalize = true)
    {
        SignalGuard.SameLength(x, y);

        if (maxLag < 0)
        {
            maxLag = x.Length - 1;
        }

        if (maxLag >= x.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLag), maxLag, "Max lag must be less than signal length.");
        }

        var result = new double[2 * maxLag + 1];
        for (var lag = -maxLag; lag <= maxLag; lag++)
        {
            var sum = 0.0;
            var count = 0;
            for (var n = 0; n < x.Length; n++)
            {
                var m = n + lag;
                if (m >= 0 && m < y.Length)
                {
                    sum += x[n] * y[m];
                    count++;
                }
            }

            result[lag + maxLag] = count > 0 ? sum / count : 0;
        }

        if (normalize)
        {
            var energyX = 0.0;
            var energyY = 0.0;
            for (var n = 0; n < x.Length; n++)
            {
                energyX += x[n] * x[n];
                energyY += y[n] * y[n];
            }

            var norm = Math.Sqrt(energyX * energyY) / x.Length;
            if (norm > 0)
            {
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] /= norm;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 计算信号的自相关函数。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="maxLag">最大滞后点数，默认为信号长度 - 1。</param>
    /// <param name="normalize">是否归一化使零滞后为 1.0，默认 <see langword="true"/>。</param>
    /// <returns>自相关数组，长度为 maxLag + 1，索引 0 为零滞后。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] AutoCorrelation(double[] data, int maxLag = -1, bool normalize = true)
    {
        SignalGuard.NotEmpty(data);

        if (maxLag < 0)
        {
            maxLag = data.Length - 1;
        }

        if (maxLag >= data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLag), maxLag, "Max lag must be less than signal length.");
        }

        return CrossCorrelation(data, data, maxLag, normalize);
    }

    /// <summary>
    /// 基于 FFT 的高效互相关（适用于长信号）。
    /// </summary>
    /// <param name="x">参考信号时域序列。</param>
    /// <param name="y">比较信号时域序列，长度须与 <paramref name="x"/> 相同。</param>
    /// <returns>互相关数组，长度 2N-1（N 为补零后 FFT 长度），零滞后位于中心。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当两信号长度不一致或为空时抛出。</exception>
    public static double[] CrossCorrelationFft(double[] x, double[] y)
    {
        SignalGuard.SameLength(x, y);
        var n = MathUtilities.NextPowerOfTwo(x.Length * 2);
        var xPadded = new Complex[n];
        var yPadded = new Complex[n];

        for (var i = 0; i < x.Length; i++)
        {
            xPadded[i] = new Complex(x[i], 0);
            yPadded[i] = new Complex(y[i], 0);
        }

        FourierTransform.TransformInPlace(xPadded, inverse: false);
        FourierTransform.TransformInPlace(yPadded, inverse: false);

        for (var i = 0; i < n; i++)
        {
            xPadded[i] = xPadded[i] * Complex.Conjugate(yPadded[i]);
        }

        FourierTransform.TransformInPlace(xPadded, inverse: true);

        var result = new double[2 * x.Length - 1];
        var offset = x.Length - 1;
        for (var i = 0; i < result.Length; i++)
        {
            var srcIndex = (i + n - offset) % n;
            result[i] = xPadded[srcIndex].Real;
        }

        return result;
    }

    /// <summary>
    /// 在互相关结果中查找峰值滞后（时延估计）。
    /// </summary>
    /// <param name="correlation">互相关数组，通常由 <see cref="CrossCorrelation"/> 生成。</param>
    /// <param name="maxLag">互相关使用的最大滞后，用于计算中心索引。</param>
    /// <param name="sampleRate">采样率，单位 Hz，用于将滞后转换为秒。</param>
    /// <returns>峰值对应的时延，单位秒；正値表示比较信号滞后于参考信号。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="correlation"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static double FindPeakDelay(double[] correlation, int maxLag, double sampleRate)
    {
        SignalGuard.NotNull(correlation);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var peakIndex = 0;
        var peakValue = double.NegativeInfinity;
        for (var i = 0; i < correlation.Length; i++)
        {
            if (correlation[i] > peakValue)
            {
                peakValue = correlation[i];
                peakIndex = i;
            }
        }

        var lagSamples = peakIndex - maxLag;
        return lagSamples / sampleRate;
    }
}
