using System.Numerics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// 频谱分析结果，包含频率轴、幅值、相位及功率谱。
/// </summary>
public sealed class SpectrumResult
{
    /// <summary>频率轴数组，单位 Hz，长度与 <see cref="Magnitude"/> 相同。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>单边幅值谱，已做窗函数相干增益归一化。</summary>
    public double[] Magnitude { get; init; } = Array.Empty<double>();

    /// <summary>相位谱，单位弧度（rad），范围 [-π, π]。</summary>
    public double[] PhaseRadians { get; init; } = Array.Empty<double>();

    /// <summary>功率谱（幅值平方）。</summary>
    public double[] Power { get; init; } = Array.Empty<double>();

    /// <summary>实际 FFT 长度（补零后的 2 的幂）。</summary>
    public int FftLength { get; init; }

    /// <summary>频率分辨率，单位 Hz，等于采样率 / FFT 长度。</summary>
    public double FrequencyResolution { get; init; }
}

/// <summary>
/// 频谱分析器，提供加窗 FFT 幅值/相位谱计算。
/// </summary>
public static class SpectrumAnalyzer
{
    /// <summary>
    /// 对时域信号执行加窗 FFT 频谱分析。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗，影响泄漏与幅值精度。</param>
    /// <param name="removeDc">是否去除直流分量（去均值），默认 <see langword="true"/>。</param>
    /// <returns>包含频率轴、幅值、相位和功率的 <see cref="SpectrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 小于等于 0 时抛出。</exception>
    public static SpectrumResult Analyze(
        double[] data,
        double sampleRate,
        WindowType windowType = WindowType.Hanning,
        bool removeDc = true)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var working = removeDc ? MathUtilities.Detrend(data) : (double[])data.Clone();
        var window = WindowGenerator.Create(working.Length, windowType);
        var coherentGain = window.Average();

        for (var i = 0; i < working.Length; i++)
        {
            working[i] *= window[i];
        }

        var spectrum = FourierTransform.Forward(working);
        var half = spectrum.Length / 2;
        var frequencies = new double[half + 1];
        var magnitude = new double[half + 1];
        var phase = new double[half + 1];
        var power = new double[half + 1];

        for (var i = 0; i <= half; i++)
        {
            frequencies[i] = i * sampleRate / spectrum.Length;
            var bin = spectrum[i];
            var scale = i == 0 || i == half ? 1.0 : 2.0;
            var normalized = bin.Magnitude / (coherentGain * working.Length);
            magnitude[i] = normalized * scale;
            phase[i] = bin.Phase;
            power[i] = magnitude[i] * magnitude[i];
        }

        return new SpectrumResult
        {
            Frequencies = frequencies,
            Magnitude = magnitude,
            PhaseRadians = phase,
            Power = power,
            FftLength = spectrum.Length,
            FrequencyResolution = sampleRate / spectrum.Length
        };
    }

    /// <summary>
    /// 在指定频率范围内查找幅值最大的主频。
    /// </summary>
    /// <param name="spectrum">频谱分析结果，由 <see cref="Analyze"/> 生成。</param>
    /// <param name="minFrequency">搜索下限频率，单位 Hz，默认 0。</param>
    /// <param name="maxFrequency">搜索上限频率，单位 Hz，默认正无穷。</param>
    /// <returns>幅值最大处对应的频率，单位 Hz；若无匹配频点则返回 0。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="spectrum"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double FindDominantFrequency(SpectrumResult spectrum, double minFrequency = 0, double maxFrequency = double.PositiveInfinity)
    {
        if (spectrum is null)
        {
            throw new ArgumentNullException(nameof(spectrum));
        }
        var maxValue = double.NegativeInfinity;
        var dominant = 0.0;

        for (var i = 0; i < spectrum.Frequencies.Length; i++)
        {
            var frequency = spectrum.Frequencies[i];
            if (frequency < minFrequency || frequency > maxFrequency)
            {
                continue;
            }

            if (spectrum.Magnitude[i] > maxValue)
            {
                maxValue = spectrum.Magnitude[i];
                dominant = frequency;
            }
        }

        return dominant;
    }
}
