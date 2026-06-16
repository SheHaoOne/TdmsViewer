using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Filtering;

/// <summary>
/// FIR 有限冲激响应滤波器，基于卷积实现，支持任意系数。
/// </summary>
public sealed class FirFilter : IFilter
{
    private readonly double[] _coefficients;
    private readonly double[] _history;
    private int _index;

    /// <summary>
    /// 使用自定义 FIR 系数初始化滤波器。
    /// </summary>
    /// <param name="coefficients">FIR 滤波器系数数组（冲激响应），须非空。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="coefficients"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="coefficients"/> 为空数组时抛出。</exception>
    public FirFilter(double[] coefficients)
    {
        SignalGuard.NotEmpty(coefficients);
        _coefficients = (double[])coefficients.Clone();
        _history = new double[_coefficients.Length];
    }

    /// <summary>
    /// 创建滑动平均（低通）FIR 滤波器。
    /// </summary>
    /// <param name="length">平均窗口长度（采样点数），须大于 0。</param>
    /// <returns>系数均为 1/length 的 FIR 滤波器实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 小于等于 0 时抛出。</exception>
    public static FirFilter CreateMovingAverage(int length)
    {
        SignalGuard.Positive(length, nameof(length));
        var coefficients = Enumerable.Repeat(1.0 / length, length).ToArray();
        return new FirFilter(coefficients);
    }

    /// <inheritdoc />
    public double ProcessSample(double sample)
    {
        _history[_index] = sample;
        _index = (_index + 1) % _history.Length;

        var output = 0.0;
        var historyIndex = _index;
        for (var i = 0; i < _coefficients.Length; i++)
        {
            historyIndex--;
            if (historyIndex < 0)
            {
                historyIndex = _history.Length - 1;
            }

            output += _coefficients[i] * _history[historyIndex];
        }

        return output;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public double[] ProcessBlock(double[] data)
    {
        SignalGuard.NotEmpty(data);
        var result = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = ProcessSample(data[i]);
        }

        return result;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_history, 0, _history.Length);
        _index = 0;
    }
}
