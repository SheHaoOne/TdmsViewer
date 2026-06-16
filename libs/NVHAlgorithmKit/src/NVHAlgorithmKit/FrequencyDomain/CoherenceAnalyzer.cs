using System.Numerics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// 相干函数分析结果，描述两通道信号在各频率上的线性相关程度。
/// </summary>
public sealed class CoherenceResult
{
    /// <summary>频率轴数组，单位 Hz。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>相干函数值，范围 [0, 1]，1 表示完全相干。</summary>
    public double[] Coherence { get; init; } = Array.Empty<double>();

    /// <summary>参与平均的段数。</summary>
    public int SegmentCount { get; init; }

    /// <summary>频率分辨率，单位 Hz。</summary>
    public double FrequencyResolution { get; init; }
}

/// <summary>
/// 双通道相干函数估计器（Welch 方法），用于传递路径识别与噪声源分析。
/// </summary>
public static class CoherenceAnalyzer
{
    /// <summary>
    /// 估计输入与输出信号之间的相干函数 γ²(f) = |Gxy|² / (Gxx · Gyy)。
    /// </summary>
    /// <param name="input">输入/激励信号时域序列（通道 x）。</param>
    /// <param name="output">输出/响应信号时域序列（通道 y），长度须与 <paramref name="input"/> 相同。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>相干函数结果 <see cref="CoherenceResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号长度不一致、为空或不足以分段时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static CoherenceResult Estimate(
        double[] input,
        double[] output,
        double sampleRate,
        int segmentLength = 1024,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning)
    {
        SignalGuard.SameLength(input, output);
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
        var gxx = new double[half + 1];
        var gyy = new double[half + 1];
        var gxyReal = new double[half + 1];
        var gxyImag = new double[half + 1];
        var segmentCount = 0;

        for (var start = 0; start + segmentLength <= input.Length; start += hop)
        {
            var segX = ExtractWindowedSegment(input, start, segmentLength, window);
            var segY = ExtractWindowedSegment(output, start, segmentLength, window);
            var specX = FourierTransform.Forward(segX);
            var specY = FourierTransform.Forward(segY);

            for (var i = 0; i <= half; i++)
            {
                var scale = i == 0 || i == half ? 1.0 : 2.0;
                var factor = scale / (windowEnergy * sampleRate);
                gxx[i] += specX[i].Magnitude * specX[i].Magnitude * factor;
                gyy[i] += specY[i].Magnitude * specY[i].Magnitude * factor;
                var cross = specX[i] * Complex.Conjugate(specY[i]);
                gxyReal[i] += cross.Real * factor;
                gxyImag[i] += cross.Imaginary * factor;
            }

            segmentCount++;
        }

        if (segmentCount == 0)
        {
            throw new ArgumentException("Signal is too short for the requested segment length.", nameof(input));
        }

        for (var i = 0; i <= half; i++)
        {
            gxx[i] /= segmentCount;
            gyy[i] /= segmentCount;
            gxyReal[i] /= segmentCount;
            gxyImag[i] /= segmentCount;
        }

        var coherence = new double[half + 1];
        for (var i = 0; i <= half; i++)
        {
            var gxyMagSq = gxyReal[i] * gxyReal[i] + gxyImag[i] * gxyImag[i];
            var denom = gxx[i] * gyy[i];
            coherence[i] = denom > 0 ? Math.Min(1.0, gxyMagSq / denom) : 0;
        }

        return new CoherenceResult
        {
            Frequencies = FourierTransform.FrequencyAxis(segmentLength, sampleRate),
            Coherence = coherence,
            SegmentCount = segmentCount,
            FrequencyResolution = sampleRate / segmentLength
        };
    }

    private static double[] ExtractWindowedSegment(double[] data, int start, int length, double[] window)
    {
        var segment = new double[length];
        Array.Copy(data, start, segment, 0, length);
        segment = MathUtilities.Detrend(segment);
        for (var i = 0; i < length; i++)
        {
            segment[i] *= window[i];
        }

        return segment;
    }
}
