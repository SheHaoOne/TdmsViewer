using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.FrequencyDomain;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.Order;

/// <summary>
/// Campbell 图（转速-频率谱阵）分析结果，用于扫频/加减速工况。
/// </summary>
public sealed class CampbellDiagramResult
{
    /// <summary>转速轴数组，单位 RPM。</summary>
    public double[] RpmAxis { get; init; } = Array.Empty<double>();

    /// <summary>频率轴数组，单位 Hz。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>幅值矩阵，第一维为转速索引，第二维为频率索引。</summary>
    public double[,] Magnitude { get; init; } = new double[0, 0];

    /// <summary>分段数量（转速帧数）。</summary>
    public int SegmentCount { get; init; }
}

/// <summary>
/// Campbell 图分析器，生成转速-频率-幅值三维数据，用于共振转速识别。
/// </summary>
public static class CampbellDiagramAnalyzer
{
    /// <summary>
    /// 根据振动信号与同步转速轨迹生成 Campbell 图数据。
    /// </summary>
    /// <param name="signal">振动时域序列（加速度或速度）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="rpmTrace">与信号逐点对齐的转速轨迹，单位 RPM。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 2048。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>Campbell 图数据 <see cref="CampbellDiagramResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号与转速轨迹长度不一致或信号过短时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static CampbellDiagramResult Compute(
        double[] signal,
        double sampleRate,
        double[] rpmTrace,
        int segmentLength = 2048,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning)
    {
        SignalGuard.NotEmpty(signal);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.NotNull(rpmTrace);
        SignalGuard.Positive(segmentLength, nameof(segmentLength));

        if (rpmTrace.Length != signal.Length)
        {
            throw new ArgumentException("RPM trace must match signal length.");
        }

        if (overlapRatio < 0 || overlapRatio >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapRatio), overlapRatio, "Overlap ratio must be in [0, 1).");
        }

        var hop = Math.Max(1, (int)Math.Round(segmentLength * (1.0 - overlapRatio)));
        var segments = new List<(double MeanRpm, double[] Data)>();

        for (var start = 0; start + segmentLength <= signal.Length; start += hop)
        {
            var meanRpm = 0.0;
            for (var i = start; i < start + segmentLength; i++)
            {
                meanRpm += rpmTrace[i];
            }

            meanRpm /= segmentLength;
            if (meanRpm <= 0)
            {
                continue;
            }

            var segment = new double[segmentLength];
            Array.Copy(signal, start, segment, 0, segmentLength);
            segments.Add((meanRpm, segment));
        }

        if (segments.Count == 0)
        {
            throw new ArgumentException("No valid segments found. Check RPM trace and segment length.", nameof(signal));
        }

        var referenceSpectrum = SpectrumAnalyzer.Analyze(segments[0].Data, sampleRate, windowType);
        var rpmAxis = new double[segments.Count];
        var magnitude = new double[segments.Count, referenceSpectrum.Frequencies.Length];

        for (var i = 0; i < segments.Count; i++)
        {
            rpmAxis[i] = segments[i].MeanRpm;
            var spectrum = SpectrumAnalyzer.Analyze(segments[i].Data, sampleRate, windowType);
            for (var j = 0; j < referenceSpectrum.Frequencies.Length; j++)
            {
                magnitude[i, j] = spectrum.Magnitude[j];
            }
        }

        return new CampbellDiagramResult
        {
            RpmAxis = rpmAxis,
            Frequencies = referenceSpectrum.Frequencies,
            Magnitude = magnitude,
            SegmentCount = segments.Count
        };
    }

    /// <summary>
    /// 从 Campbell 图中提取指定阶次轨迹的幅值随转速变化曲线。
    /// </summary>
    /// <param name="result">Campbell 图分析结果。</param>
    /// <param name="order">目标阶次（如 1 阶=转频，2 阶=2 倍转频）。</param>
    /// <returns>与 <see cref="CampbellDiagramResult.RpmAxis"/> 等长的阶次幅值数组。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="result"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double[] ExtractOrderTrack(CampbellDiagramResult result, double order)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var track = new double[result.SegmentCount];
        for (var i = 0; i < result.SegmentCount; i++)
        {
            var rpm = result.RpmAxis[i];
            var targetFreq = OrderAnalyzer.RpmToFrequency(rpm, order);
            track[i] = InterpolateMagnitude(result, targetFreq, i);
        }

        return track;
    }

    private static double InterpolateMagnitude(CampbellDiagramResult result, double frequency, int rpmIndex)
    {
        var frequencies = result.Frequencies;
        if (frequency <= frequencies[0] || frequency >= frequencies[frequencies.Length - 1])
        {
            return 0;
        }

        var index = Array.BinarySearch(frequencies, frequency);
        if (index >= 0)
        {
            return result.Magnitude[rpmIndex, index];
        }

        index = ~index;
        var x0 = frequencies[index - 1];
        var x1 = frequencies[index];
        var y0 = result.Magnitude[rpmIndex, index - 1];
        var y1 = result.Magnitude[rpmIndex, index];
        var ratio = (frequency - x0) / (x1 - x0);
        return y0 + ratio * (y1 - y0);
    }
}
