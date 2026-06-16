namespace NVHAlgorithmKit.Filtering;

/// <summary>
/// 二阶节（Biquad）IIR 滤波器，采用 Direct Form II Transposed 结构实现。
/// </summary>
public sealed class BiquadFilter : IFilter
{
    private readonly double _b0;
    private readonly double _b1;
    private readonly double _b2;
    private readonly double _a1;
    private readonly double _a2;
    private double _z1;
    private double _z2;

    /// <summary>
    /// 使用传递函数系数初始化二阶 IIR 滤波器，内部自动将 a0 归一化为 1。
    /// </summary>
    /// <param name="b0">分子系数 b0（零点部分常数项）。</param>
    /// <param name="b1">分子系数 b1（零点部分一次项）。</param>
    /// <param name="b2">分子系数 b2（零点部分二次项）。</param>
    /// <param name="a0">分母系数 a0（极点部分常数项，须非零）。</param>
    /// <param name="a1">分母系数 a1（极点部分一次项）。</param>
    /// <param name="a2">分母系数 a2（极点部分二次项）。</param>
    public BiquadFilter(double b0, double b1, double b2, double a0, double a1, double a2)
    {
        _b0 = b0 / a0;
        _b1 = b1 / a0;
        _b2 = b2 / a0;
        _a1 = a1 / a0;
        _a2 = a2 / a0;
    }

    /// <inheritdoc />
    public double ProcessSample(double sample)
    {
        var output = _b0 * sample + _z1;
        _z1 = _b1 * sample - _a1 * output + _z2;
        _z2 = _b2 * sample - _a2 * output;
        return output;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    public double[] ProcessBlock(double[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
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
        _z1 = 0;
        _z2 = 0;
    }
}
