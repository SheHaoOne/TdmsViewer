using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.FrequencyDomain;
using NVHAlgorithmKit.TimeDomain;

namespace NVHAlgorithmKit.Order;

/// <summary>
/// 转速提取结果。
/// </summary>
public sealed class RpmExtractionResult
{
    /// <summary>逐采样点 RPM 轨迹，与输入信号等长，单位 RPM。</summary>
    public double[] RpmTrace { get; init; } = Array.Empty<double>();

    /// <summary>平均转速，单位 RPM。</summary>
    public double MeanRpm { get; init; }

    /// <summary>检测到的每转脉冲数。</summary>
    public double PulsesPerRevolution { get; init; }
}

/// <summary>
/// 转速（RPM）提取器，支持键相脉冲与振动信号估算。
/// </summary>
public static class RpmExtractor
{
    /// <summary>
    /// 从键相/转速脉冲信号提取逐点 RPM 轨迹。
    /// </summary>
    /// <param name="tachometer">键相脉冲时域信号（方波或脉冲串），每个脉冲对应 partial 转。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="pulsesPerRevolution">每转脉冲数，默认 1（每转一个脉冲）。</param>
    /// <param name="smoothingWindow">RPM 平滑窗口（采样点数），默认 0（不平滑）。</param>
    /// <returns>转速提取结果 <see cref="RpmExtractionResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="tachometer"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="tachometer"/> 为空数组时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sampleRate"/> 或 <paramref name="pulsesPerRevolution"/> 无效时抛出。</exception>
    public static RpmExtractionResult FromTachometer(
        double[] tachometer,
        double sampleRate,
        double pulsesPerRevolution = 1,
        int smoothingWindow = 0)
    {
        SignalGuard.NotEmpty(tachometer);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(pulsesPerRevolution, nameof(pulsesPerRevolution));

        var edges = DetectRisingEdges(tachometer);
        var rpmTrace = new double[tachometer.Length];
        var lastEdge = -1;

        for (var i = 0; i < tachometer.Length; i++)
        {
            while (lastEdge + 1 < edges.Count && edges[lastEdge + 1] <= i)
            {
                lastEdge++;
            }

            if (lastEdge < 1)
            {
                rpmTrace[i] = 0;
                continue;
            }

            var period = (edges[lastEdge] - edges[lastEdge - 1]) / sampleRate;
            rpmTrace[i] = period > 0 ? 60.0 / (period * pulsesPerRevolution) : 0;
        }

        if (smoothingWindow > 1)
        {
            rpmTrace = MovingAverage(rpmTrace, smoothingWindow);
        }

        var meanRpm = rpmTrace.Where(v => v > 0).DefaultIfEmpty(0).Average();
        return new RpmExtractionResult
        {
            RpmTrace = rpmTrace,
            MeanRpm = meanRpm,
            PulsesPerRevolution = pulsesPerRevolution
        };
    }

    /// <summary>
    /// 从振动信号估算主转速（假设振动主频为 1 倍转频）。
    /// </summary>
    /// <param name="vibration">振动时域序列（加速度或速度）。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="minRpm">搜索最低转速，单位 RPM，默认 60。</param>
    /// <param name="maxRpm">搜索最高转速，单位 RPM，默认 10000。</param>
    /// <returns>估算的主转速，单位 RPM。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="vibration"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="vibration"/> 为空数组时抛出。</exception>
    public static double EstimateFromVibration(
        double[] vibration,
        double sampleRate,
        double minRpm = 60,
        double maxRpm = 10000)
    {
        SignalGuard.NotEmpty(vibration);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));

        var minFreq = minRpm / 60.0;
        var maxFreq = maxRpm / 60.0;
        var spectrum = SpectrumAnalyzer.Analyze(vibration, sampleRate);
        var dominantFreq = SpectrumAnalyzer.FindDominantFrequency(spectrum, minFreq, maxFreq);
        return dominantFreq * 60.0;
    }

    /// <summary>
    /// 从振动信号生成逐段 RPM 轨迹（分段频谱主频法）。
    /// </summary>
    /// <param name="vibration">振动时域序列。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每段分析长度（采样点数），默认 4096。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="minRpm">搜索最低转速，单位 RPM，默认 60。</param>
    /// <param name="maxRpm">搜索最高转速，单位 RPM，默认 10000。</param>
    /// <returns>与振动信号等长的 RPM 轨迹（段内插值填充）。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="vibration"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号过短时抛出。</exception>
    public static double[] TraceFromVibration(
        double[] vibration,
        double sampleRate,
        int segmentLength = 4096,
        double overlapRatio = 0.5,
        double minRpm = 60,
        double maxRpm = 10000)
    {
        SignalGuard.NotEmpty(vibration);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(segmentLength, nameof(segmentLength));

        var hop = Math.Max(1, (int)Math.Round(segmentLength * (1.0 - overlapRatio)));
        var rpmTrace = new double[vibration.Length];
        var minFreq = minRpm / 60.0;
        var maxFreq = maxRpm / 60.0;

        for (var start = 0; start + segmentLength <= vibration.Length; start += hop)
        {
            var segment = new double[segmentLength];
            Array.Copy(vibration, start, segment, 0, segmentLength);
            var spectrum = SpectrumAnalyzer.Analyze(segment, sampleRate);
            var freq = SpectrumAnalyzer.FindDominantFrequency(spectrum, minFreq, maxFreq);
            var rpm = freq * 60.0;
            var end = Math.Min(start + hop, vibration.Length);
            for (var i = start; i < end; i++)
            {
                rpmTrace[i] = rpm;
            }
        }

        return rpmTrace;
    }

    private static List<int> DetectRisingEdges(double[] signal)
    {
        var edges = new List<int>();
        var threshold = (signal.Max() + signal.Min()) / 2.0;
        for (var i = 1; i < signal.Length; i++)
        {
            if (signal[i - 1] < threshold && signal[i] >= threshold)
            {
                edges.Add(i);
            }
        }

        return edges;
    }

    private static double[] MovingAverage(double[] data, int window)
    {
        var result = new double[data.Length];
        var half = window / 2;
        for (var i = 0; i < data.Length; i++)
        {
            var sum = 0.0;
            var count = 0;
            for (var j = Math.Max(0, i - half); j < Math.Min(data.Length, i + half); j++)
            {
                sum += data[j];
                count++;
            }

            result[i] = count > 0 ? sum / count : 0;
        }

        return result;
    }
}
