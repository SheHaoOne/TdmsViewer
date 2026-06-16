using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Windows;

/// <summary>
/// 窗函数生成器，用于频谱分析前的加窗处理及幅值修正。
/// </summary>
public static class WindowGenerator
{
    /// <summary>
    /// 生成指定类型和长度的窗函数系数数组。
    /// </summary>
    /// <param name="length">窗长度（采样点数），须大于 0，通常与 FFT 段长一致。</param>
    /// <param name="type">窗函数类型，参见 <see cref="WindowType"/>。</param>
    /// <param name="kaiserBeta">Kaiser 窗的形状参数 Beta，仅当 <paramref name="type"/> 为 <see cref="WindowType.Kaiser"/> 时生效；值越大旁瓣越低，默认 8.6。</param>
    /// <returns>长度为 <paramref name="length"/> 的窗系数数组，取值范围 [0, 1]。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 小于等于 0，或 <paramref name="type"/> 无效时抛出。</exception>
    public static double[] Create(int length, WindowType type, double kaiserBeta = 8.6)
    {
        SignalGuard.Positive(length, nameof(length));
        return type switch
        {
            WindowType.Rectangular => CreateRectangular(length),
            WindowType.Hanning => CreateHanning(length),
            WindowType.Hamming => CreateHamming(length),
            WindowType.Blackman => CreateBlackman(length),
            WindowType.BlackmanHarris => CreateBlackmanHarris(length),
            WindowType.FlatTop => CreateFlatTop(length),
            WindowType.Kaiser => CreateKaiser(length, kaiserBeta),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported window type.")
        };
    }

    /// <summary>
    /// 计算窗函数的相干增益（系数均值），用于幅值归一化补偿。
    /// </summary>
    /// <param name="type">窗函数类型。</param>
    /// <param name="length">窗长度（采样点数），须大于 0。</param>
    /// <param name="kaiserBeta">Kaiser 窗 Beta 参数，默认 8.6。</param>
    /// <returns>窗系数算术平均值（相干增益）。</returns>
    public static double CoherentGain(WindowType type, int length, double kaiserBeta = 8.6)
    {
        var window = Create(length, type, kaiserBeta);
        return window.Average();
    }

    /// <summary>
    /// 计算窗函数的幅值修正因子（系数最大值），用于峰值幅值校准。
    /// </summary>
    /// <param name="type">窗函数类型。</param>
    /// <param name="length">窗长度（采样点数），须大于 0。</param>
    /// <param name="kaiserBeta">Kaiser 窗 Beta 参数，默认 8.6。</param>
    /// <returns>窗系数的最大值。</returns>
    public static double AmplitudeCorrectionFactor(WindowType type, int length, double kaiserBeta = 8.6)
    {
        var window = Create(length, type, kaiserBeta);
        return window.Max();
    }

    /// <summary>
    /// 对时域信号逐点乘以窗函数系数。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="type">窗函数类型。</param>
    /// <param name="kaiserBeta">Kaiser 窗 Beta 参数，默认 8.6。</param>
    /// <returns>加窗后的新数组，长度与 <paramref name="data"/> 相同。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    public static double[] Apply(double[] data, WindowType type, double kaiserBeta = 8.6)
    {
        SignalGuard.NotEmpty(data);
        var window = Create(data.Length, type, kaiserBeta);
        var result = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = data[i] * window[i];
        }

        return result;
    }

    private static double[] CreateRectangular(int length)
    {
        var window = new double[length];
        for (var i = 0; i < length; i++)
        {
            window[i] = 1.0;
        }

        return window;
    }

    private static double[] CreateHanning(int length)
    {
        var window = new double[length];
        if (length == 1)
        {
            window[0] = 1.0;
            return window;
        }

        for (var i = 0; i < length; i++)
        {
            window[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (length - 1)));
        }

        return window;
    }

    private static double[] CreateHamming(int length)
    {
        var window = new double[length];
        if (length == 1)
        {
            window[0] = 1.0;
            return window;
        }

        for (var i = 0; i < length; i++)
        {
            window[i] = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / (length - 1));
        }

        return window;
    }

    private static double[] CreateBlackman(int length)
    {
        var window = new double[length];
        if (length == 1)
        {
            window[0] = 1.0;
            return window;
        }

        for (var i = 0; i < length; i++)
        {
            var phase = 2.0 * Math.PI * i / (length - 1);
            window[i] = 0.42 - 0.5 * Math.Cos(phase) + 0.08 * Math.Cos(2.0 * phase);
        }

        return window;
    }

    private static double[] CreateBlackmanHarris(int length)
    {
        var window = new double[length];
        if (length == 1)
        {
            window[0] = 1.0;
            return window;
        }

        const double a0 = 0.35875;
        const double a1 = 0.48829;
        const double a2 = 0.14128;
        const double a3 = 0.01168;

        for (var i = 0; i < length; i++)
        {
            var phase = 2.0 * Math.PI * i / (length - 1);
            window[i] = a0
                        - a1 * Math.Cos(phase)
                        + a2 * Math.Cos(2.0 * phase)
                        - a3 * Math.Cos(3.0 * phase);
        }

        return window;
    }

    private static double[] CreateFlatTop(int length)
    {
        var window = new double[length];
        if (length == 1)
        {
            window[0] = 1.0;
            return window;
        }

        const double a0 = 0.21557895;
        const double a1 = 0.41663158;
        const double a2 = 0.277263158;
        const double a3 = 0.083578947;
        const double a4 = 0.006947368;

        for (var i = 0; i < length; i++)
        {
            var phase = 2.0 * Math.PI * i / (length - 1);
            window[i] = a0
                        - a1 * Math.Cos(phase)
                        + a2 * Math.Cos(2.0 * phase)
                        - a3 * Math.Cos(3.0 * phase)
                        + a4 * Math.Cos(4.0 * phase);
        }

        return window;
    }

    private static double[] CreateKaiser(int length, double beta)
    {
        var window = new double[length];
        var denominator = BesselI0(beta);
        var half = (length - 1) / 2.0;

        for (var i = 0; i < length; i++)
        {
            var radius = (i - half) / half;
            var value = beta * Math.Sqrt(Math.Max(0.0, 1.0 - radius * radius));
            window[i] = BesselI0(value) / denominator;
        }

        return window;
    }

    private static double BesselI0(double x)
    {
        var sum = 1.0;
        var term = 1.0;
        for (var k = 1; k <= 25; k++)
        {
            term *= (x * x) / (4.0 * k * k);
            sum += term;
            if (term < 1e-12 * sum)
            {
                break;
            }
        }

        return sum;
    }
}
