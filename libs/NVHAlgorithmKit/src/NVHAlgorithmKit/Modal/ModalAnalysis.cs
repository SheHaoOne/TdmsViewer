using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.FrequencyDomain;

namespace NVHAlgorithmKit.Modal;

/// <summary>
/// 单个模态峰值参数。
/// </summary>
public sealed class ModalPeak
{
    /// <summary>固有频率（共振峰频率），单位 Hz。</summary>
    public double NaturalFrequency { get; init; }

    /// <summary>阻尼比 ζ（无量纲），由半功率带宽法估算。</summary>
    public double DampingRatio { get; init; }

    /// <summary>共振峰幅值 |H(fₙ)|。</summary>
    public double Magnitude { get; init; }

    /// <summary>半功率带宽 Δf = f₂ - f₁，单位 Hz。</summary>
    public double HalfPowerBandwidth { get; init; }
}

/// <summary>
/// 模态参数识别结果。
/// </summary>
public sealed class ModalAnalysisResult
{
    /// <summary>识别到的模态峰值列表，按频率升序排列。</summary>
    public IReadOnlyList<ModalPeak> Peaks { get; init; } = Array.Empty<ModalPeak>();

    /// <summary>频率分辨率，单位 Hz。</summary>
    public double FrequencyResolution { get; init; }
}

/// <summary>
/// 模态参数识别器，从 FRF 或频谱中自动拾取共振峰并估算阻尼比。
/// </summary>
public static class ModalParameterIdentifier
{
    /// <summary>
    /// 从频率响应函数中识别模态参数（峰值拾取 + 半功率带宽法）。
    /// </summary>
    /// <param name="frequencies">频率轴，单位 Hz。</param>
    /// <param name="magnitude">FRF 幅值 |H(f)|。</param>
    /// <param name="peakThreshold">峰值检测阈值，相对于全局最大幅值的比例，默认 0.1（10%）。</param>
    /// <param name="minPeakDistanceHz">相邻峰最小频率间隔，单位 Hz，默认 5 Hz，用于抑制重复拾取。</param>
    /// <returns>模态识别结果 <see cref="ModalAnalysisResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当数组为空或长度不一致时抛出。</exception>
    public static ModalAnalysisResult IdentifyFromFrf(
        double[] frequencies,
        double[] magnitude,
        double peakThreshold = 0.1,
        double minPeakDistanceHz = 5.0)
    {
        SignalGuard.NotNull(frequencies);
        SignalGuard.NotNull(magnitude);
        if (frequencies.Length == 0 || frequencies.Length != magnitude.Length)
        {
            throw new ArgumentException("Frequency and magnitude arrays must be non-empty and equal length.");
        }

        if (peakThreshold <= 0 || peakThreshold > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(peakThreshold), peakThreshold, "Threshold must be in (0, 1].");
        }

        var globalMax = magnitude.Max();
        var threshold = globalMax * peakThreshold;
        var peaks = new List<ModalPeak>();

        for (var i = 1; i < magnitude.Length - 1; i++)
        {
            if (magnitude[i] < threshold)
            {
                continue;
            }

            if (magnitude[i] <= magnitude[i - 1] || magnitude[i] <= magnitude[i + 1])
            {
                continue;
            }

            var fn = frequencies[i];
            if (peaks.Any(p => Math.Abs(p.NaturalFrequency - fn) < minPeakDistanceHz))
            {
                continue;
            }

            var halfPower = magnitude[i] / Math.Sqrt(2);
            var f1 = FindCrossing(frequencies, magnitude, i, -1, halfPower);
            var f2 = FindCrossing(frequencies, magnitude, i, 1, halfPower);
            var bandwidth = f2 - f1;
            var damping = fn > 0 ? bandwidth / (2.0 * fn) : 0;

            peaks.Add(new ModalPeak
            {
                NaturalFrequency = fn,
                DampingRatio = damping,
                Magnitude = magnitude[i],
                HalfPowerBandwidth = bandwidth
            });
        }

        peaks.Sort((a, b) => a.NaturalFrequency.CompareTo(b.NaturalFrequency));

        var resolution = frequencies.Length > 1 ? frequencies[1] - frequencies[0] : 0;
        return new ModalAnalysisResult
        {
            Peaks = peaks,
            FrequencyResolution = resolution
        };
    }

    /// <summary>
    /// 从频谱结果中识别模态参数。
    /// </summary>
    /// <param name="spectrum">频谱分析结果。</param>
    /// <param name="peakThreshold">峰值检测阈值，默认 0.1。</param>
    /// <param name="minPeakDistanceHz">相邻峰最小频率间隔，单位 Hz，默认 5 Hz。</param>
    /// <returns>模态识别结果。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="spectrum"/> 为 <see langword="null"/> 时抛出。</exception>
    public static ModalAnalysisResult IdentifyFromSpectrum(
        SpectrumResult spectrum,
        double peakThreshold = 0.1,
        double minPeakDistanceHz = 5.0)
    {
        if (spectrum is null)
        {
            throw new ArgumentNullException(nameof(spectrum));
        }

        return IdentifyFromFrf(spectrum.Frequencies, spectrum.Magnitude, peakThreshold, minPeakDistanceHz);
    }

    private static double FindCrossing(double[] frequencies, double[] magnitude, int peakIndex, int direction, double level)
    {
        var i = peakIndex;
        while (i > 0 && i < magnitude.Length - 1)
        {
            i += direction;
            if (magnitude[i] <= level)
            {
                var i0 = i - direction;
                if (i0 < 0 || i0 >= magnitude.Length)
                {
                    return frequencies[i];
                }

                var y0 = magnitude[i0] - level;
                var y1 = magnitude[i] - level;
                var ratio = y0 / (y0 - y1);
                return frequencies[i0] + ratio * (frequencies[i] - frequencies[i0]);
            }
        }

        var clamped = i < 0 ? 0 : i >= frequencies.Length ? frequencies.Length - 1 : i;
        return frequencies[clamped];
    }
}
