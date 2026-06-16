namespace NVHAlgorithmKit.Filtering;

/// <summary>
/// IIR 滤波器类型。
/// </summary>
public enum FilterType
{
    /// <summary>低通滤波器，通过低于截止频率的分量。</summary>
    LowPass,

    /// <summary>高通滤波器，通过高于截止频率的分量。</summary>
    HighPass,

    /// <summary>带通滤波器，通过指定频带内的分量。</summary>
    BandPass,

    /// <summary>带阻滤波器，抑制指定频带内的分量。</summary>
    BandStop
}

/// <summary>
/// 双线性变换 Butterworth 滤波器设计器，生成最大平坦通带响应的 IIR 滤波器。
/// </summary>
public static class ButterworthDesigner
{
    /// <summary>
    /// 设计 Butterworth 低通滤波器。
    /// </summary>
    /// <param name="order">滤波器阶数，须大于等于 1；阶数越高过渡带越陡。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="cutoffFrequency">-3 dB 截止频率，单位 Hz，须满足 0 &lt; fc &lt; Nyquist（sampleRate/2）。</param>
    /// <returns>级联二阶节组成的 <see cref="IFilter"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static IFilter CreateLowPass(int order, double sampleRate, double cutoffFrequency)
    {
        Validate(order, sampleRate, cutoffFrequency);
        var sections = Design(order, FilterType.LowPass, sampleRate, cutoffFrequency, cutoffFrequency * 2);
        return new CascadedFilter(sections);
    }

    /// <summary>
    /// 设计 Butterworth 高通滤波器。
    /// </summary>
    /// <param name="order">滤波器阶数，须大于等于 1。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="cutoffFrequency">-3 dB 截止频率，单位 Hz，须满足 0 &lt; fc &lt; Nyquist。</param>
    /// <returns>级联二阶节组成的 <see cref="IFilter"/> 实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static IFilter CreateHighPass(int order, double sampleRate, double cutoffFrequency)
    {
        Validate(order, sampleRate, cutoffFrequency);
        var sections = Design(order, FilterType.HighPass, sampleRate, cutoffFrequency, cutoffFrequency * 2);
        return new CascadedFilter(sections);
    }

    /// <summary>
    /// 设计 Butterworth 带通滤波器。
    /// </summary>
    /// <param name="order">滤波器阶数，须大于等于 1。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="lowCutoff">下截止频率，单位 Hz，须大于 0。</param>
    /// <param name="highCutoff">上截止频率，单位 Hz，须大于 <paramref name="lowCutoff"/> 且小于 Nyquist。</param>
    /// <returns>级联二阶节组成的 <see cref="IFilter"/> 实例。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="highCutoff"/> 不大于 <paramref name="lowCutoff"/> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当频率或阶数超出有效范围时抛出。</exception>
    public static IFilter CreateBandPass(int order, double sampleRate, double lowCutoff, double highCutoff)
    {
        Validate(order, sampleRate, lowCutoff);
        if (highCutoff <= lowCutoff)
        {
            throw new ArgumentException("High cutoff must be greater than low cutoff.");
        }

        if (highCutoff >= sampleRate / 2)
        {
            throw new ArgumentOutOfRangeException(nameof(highCutoff), highCutoff, "High cutoff must be below Nyquist frequency.");
        }

        var sections = Design(order, FilterType.BandPass, sampleRate, lowCutoff, highCutoff);
        return new CascadedFilter(sections);
    }

    private static void Validate(int order, double sampleRate, double cutoffFrequency)
    {
        if (order < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Order must be at least 1.");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Sample rate must be positive.");
        }

        if (cutoffFrequency <= 0 || cutoffFrequency >= sampleRate / 2)
        {
            throw new ArgumentOutOfRangeException(nameof(cutoffFrequency), cutoffFrequency, "Cutoff must be within (0, Nyquist).");
        }
    }

    private static BiquadFilter[] Design(int order, FilterType type, double sampleRate, double f1, double f2)
    {
        var sectionCount = (order + 1) / 2;
        var sections = new BiquadFilter[sectionCount];
        for (var k = 0; k < sectionCount; k++)
        {
            var theta = Math.PI * (2.0 * k + 1) / (2.0 * order);
            sections[k] = CreateSection(type, sampleRate, f1, f2, theta);
        }

        return sections;
    }

    private static BiquadFilter CreateSection(FilterType type, double sampleRate, double f1, double f2, double theta)
    {
        var q = 1.0 / (2.0 * Math.Cos(theta));
        return type switch
        {
            FilterType.LowPass => CreateLowPassSection(sampleRate, f1, q),
            FilterType.HighPass => CreateHighPassSection(sampleRate, f1, q),
            FilterType.BandPass => CreateBandPassSection(sampleRate, f1, f2, q),
            _ => throw new NotSupportedException($"Filter type {type} is not supported.")
        };
    }

    private static BiquadFilter CreateLowPassSection(double sampleRate, double cutoff, double q)
    {
        var w0 = 2.0 * Math.PI * cutoff / sampleRate;
        var cosW0 = Math.Cos(w0);
        var sinW0 = Math.Sin(w0);
        var alpha = sinW0 / (2.0 * q);

        var b0 = (1.0 - cosW0) / 2.0;
        var b1 = 1.0 - cosW0;
        var b2 = (1.0 - cosW0) / 2.0;
        var a0 = 1.0 + alpha;
        var a1 = -2.0 * cosW0;
        var a2 = 1.0 - alpha;
        return new BiquadFilter(b0, b1, b2, a0, a1, a2);
    }

    private static BiquadFilter CreateHighPassSection(double sampleRate, double cutoff, double q)
    {
        var w0 = 2.0 * Math.PI * cutoff / sampleRate;
        var cosW0 = Math.Cos(w0);
        var sinW0 = Math.Sin(w0);
        var alpha = sinW0 / (2.0 * q);

        var b0 = (1.0 + cosW0) / 2.0;
        var b1 = -(1.0 + cosW0);
        var b2 = (1.0 + cosW0) / 2.0;
        var a0 = 1.0 + alpha;
        var a1 = -2.0 * cosW0;
        var a2 = 1.0 - alpha;
        return new BiquadFilter(b0, b1, b2, a0, a1, a2);
    }

    private static BiquadFilter CreateBandPassSection(double sampleRate, double lowCutoff, double highCutoff, double q)
    {
        var center = Math.Sqrt(lowCutoff * highCutoff);
        var bandwidth = highCutoff - lowCutoff;
        var w0 = 2.0 * Math.PI * center / sampleRate;
        var cosW0 = Math.Cos(w0);
        var sinW0 = Math.Sin(w0);
        var alpha = sinW0 * Math.Sinh(Math.Log(2.0) / 2.0 * bandwidth / center * w0 / sinW0);

        var b0 = alpha;
        var b1 = 0.0;
        var b2 = -alpha;
        var a0 = 1.0 + alpha;
        var a1 = -2.0 * cosW0;
        var a2 = 1.0 - alpha;
        return new BiquadFilter(b0, b1, b2, a0, a1, a2);
    }
}

/// <summary>
/// 级联多个二阶节（Biquad）组成高阶 IIR 滤波器。
/// </summary>
public sealed class CascadedFilter : IFilter
{
    private readonly BiquadFilter[] _sections;

    /// <summary>
    /// 使用一组二阶节初始化级联滤波器。
    /// </summary>
    /// <param name="sections">二阶节数组，须至少包含一个元素。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="sections"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="sections"/> 为空数组时抛出。</exception>
    public CascadedFilter(BiquadFilter[] sections)
    {
        _sections = sections ?? throw new ArgumentNullException(nameof(sections));
        if (_sections.Length == 0)
        {
            throw new ArgumentException("At least one section is required.", nameof(sections));
        }
    }

    /// <inheritdoc />
    public double ProcessSample(double sample)
    {
        var output = sample;
        foreach (var section in _sections)
        {
            output = section.ProcessSample(output);
        }

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
        var result = (double[])data.Clone();
        foreach (var section in _sections)
        {
            result = section.ProcessBlock(result);
        }

        return result;
    }

    /// <inheritdoc />
    public void Reset()
    {
        foreach (var section in _sections)
        {
            section.Reset();
        }
    }
}
