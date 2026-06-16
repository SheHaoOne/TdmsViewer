using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// Welch 方法功率谱密度（PSD）估计结果。
/// </summary>
public sealed class PsdResult
{
    /// <summary>频率轴数组，单位 Hz。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>功率谱密度值，单位取决于输入信号物理量的平方（如 Pa²/Hz）。</summary>
    public double[] Density { get; init; } = Array.Empty<double>();

    /// <summary>参与平均的段数。</summary>
    public int SegmentCount { get; init; }

    /// <summary>每段的长度（采样点数）。</summary>
    public int SegmentLength { get; init; }

    /// <summary>频率分辨率，单位 Hz，等于采样率 / 段长。</summary>
    public double FrequencyResolution { get; init; }
}

/// <summary>
/// Welch 方法功率谱密度估计器，通过分段加窗平均降低方差。
/// </summary>
public static class PowerSpectralDensity
{
    /// <summary>
    /// 使用 Welch 方法估计功率谱密度。
    /// </summary>
    /// <param name="data">输入时域采样序列，长度须大于等于 <paramref name="segmentLength"/>。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每段 FFT 长度（采样点数），默认 1024，须为 2 的幂以获得最佳性能。</param>
    /// <param name="overlapRatio">段间重叠比例，范围 [0, 1)，默认 0.5（50% 重叠）。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>功率谱密度估计结果 <see cref="PsdResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号长度不足以分段时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/>、<paramref name="segmentLength"/> 无效，或 <paramref name="overlapRatio"/> 不在 [0, 1) 时抛出。</exception>
    public static PsdResult EstimateWelch(
        double[] data,
        double sampleRate,
        int segmentLength = 1024,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(segmentLength, nameof(segmentLength));

        if (overlapRatio < 0 || overlapRatio >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapRatio), overlapRatio, "Overlap ratio must be in [0, 1).");
        }

        var window = WindowGenerator.Create(segmentLength, windowType);
        var windowEnergy = window.Sum(v => v * v);
        var hop = Math.Max(1, (int)Math.Round(segmentLength * (1.0 - overlapRatio)));
        var half = segmentLength / 2;
        var psd = new double[half + 1];
        var segmentCount = 0;

        for (var start = 0; start + segmentLength <= data.Length; start += hop)
        {
            var segment = new double[segmentLength];
            Array.Copy(data, start, segment, 0, segmentLength);
            segment = MathUtilities.Detrend(segment);

            for (var i = 0; i < segmentLength; i++)
            {
                segment[i] *= window[i];
            }

            var spectrum = Transform.FourierTransform.Forward(segment);
            for (var i = 0; i <= half; i++)
            {
                var scale = i == 0 || i == half ? 1.0 : 2.0;
                var magnitudeSquared = spectrum[i].Magnitude * spectrum[i].Magnitude;
                psd[i] += scale * magnitudeSquared / (windowEnergy * sampleRate);
            }

            segmentCount++;
        }

        if (segmentCount == 0)
        {
            throw new ArgumentException("Signal is too short for the requested segment length.", nameof(data));
        }

        for (var i = 0; i < psd.Length; i++)
        {
            psd[i] /= segmentCount;
        }

        var frequencies = Transform.FourierTransform.FrequencyAxis(segmentLength, sampleRate);
        return new PsdResult
        {
            Frequencies = frequencies,
            Density = psd,
            SegmentCount = segmentCount,
            SegmentLength = segmentLength,
            FrequencyResolution = sampleRate / segmentLength
        };
    }
}
