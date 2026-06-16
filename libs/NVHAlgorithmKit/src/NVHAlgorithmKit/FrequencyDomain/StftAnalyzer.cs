using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// 短时傅里叶变换（STFT）分析结果，用于时频瀑布图与瞬态分析。
/// </summary>
public sealed class StftResult
{
    /// <summary>时间轴数组，单位秒，长度等于帧数。</summary>
    public double[] TimeAxis { get; init; } = Array.Empty<double>();

    /// <summary>频率轴数组，单位 Hz，长度等于频率 bin 数。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>
    /// 幅值谱矩阵，第一维为时间帧索引，第二维为频率索引，单位与输入信号一致。
    /// </summary>
    public double[,] Magnitude { get; init; } = new double[0, 0];

    /// <summary>每帧 FFT 段长（采样点数）。</summary>
    public int SegmentLength { get; init; }

    /// <summary>帧移（hop），单位秒。</summary>
    public double HopSeconds { get; init; }

    /// <summary>帧数。</summary>
    public int FrameCount => TimeAxis.Length;
}

/// <summary>
/// 短时傅里叶变换（STFT）分析器，生成时频幅值谱。
/// </summary>
public static class StftAnalyzer
{
    /// <summary>
    /// 对时域信号执行 STFT 分析。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每帧 FFT 长度（采样点数），默认 512，须大于 0。</param>
    /// <param name="overlapRatio">帧间重叠比例，范围 [0, 1)，默认 0.75（75% 重叠）。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <param name="removeDc">是否对每帧去除直流分量，默认 <see langword="true"/>。</param>
    /// <returns>STFT 时频分析结果 <see cref="StftResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号长度不足以分帧时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static StftResult Analyze(
        double[] data,
        double sampleRate,
        int segmentLength = 512,
        double overlapRatio = 0.75,
        WindowType windowType = WindowType.Hanning,
        bool removeDc = true)
    {
        SignalGuard.NotEmpty(data);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(segmentLength, nameof(segmentLength));

        if (overlapRatio < 0 || overlapRatio >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapRatio), overlapRatio, "Overlap ratio must be in [0, 1).");
        }

        var window = WindowGenerator.Create(segmentLength, windowType);
        var coherentGain = window.Average();
        var hop = Math.Max(1, (int)Math.Round(segmentLength * (1.0 - overlapRatio)));
        var half = segmentLength / 2;
        var frameCount = 0;
        for (var start = 0; start + segmentLength <= data.Length; start += hop)
        {
            frameCount++;
        }

        if (frameCount == 0)
        {
            throw new ArgumentException("Signal is too short for the requested segment length.", nameof(data));
        }

        var frequencies = FourierTransform.FrequencyAxis(segmentLength, sampleRate);
        var timeAxis = new double[frameCount];
        var magnitude = new double[frameCount, half + 1];
        var frameIndex = 0;

        for (var start = 0; start + segmentLength <= data.Length; start += hop)
        {
            timeAxis[frameIndex] = (start + segmentLength / 2.0) / sampleRate;

            var segment = new double[segmentLength];
            Array.Copy(data, start, segment, 0, segmentLength);
            if (removeDc)
            {
                segment = MathUtilities.Detrend(segment);
            }

            for (var i = 0; i < segmentLength; i++)
            {
                segment[i] *= window[i];
            }

            var spectrum = FourierTransform.Forward(segment);
            for (var i = 0; i <= half; i++)
            {
                var scale = i == 0 || i == half ? 1.0 : 2.0;
                magnitude[frameIndex, i] = spectrum[i].Magnitude / (coherentGain * segmentLength) * scale;
            }

            frameIndex++;
        }

        return new StftResult
        {
            TimeAxis = timeAxis,
            Frequencies = frequencies,
            Magnitude = magnitude,
            SegmentLength = segmentLength,
            HopSeconds = hop / sampleRate
        };
    }
}
