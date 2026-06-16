using System.Numerics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.FrequencyDomain;

/// <summary>
/// 频率响应函数（FRF）估计结果。
/// </summary>
public sealed class TransferFunctionResult
{
    /// <summary>频率轴数组，单位 Hz。</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>幅值响应 |H(f)|。</summary>
    public double[] Magnitude { get; init; } = Array.Empty<double>();

    /// <summary>相位响应，单位弧度。</summary>
    public double[] PhaseRadians { get; init; } = Array.Empty<double>();

    /// <summary>实部 Re[H(f)]。</summary>
    public double[] Real { get; init; } = Array.Empty<double>();

    /// <summary>虚部 Im[H(f)]。</summary>
    public double[] Imaginary { get; init; } = Array.Empty<double>();

    /// <summary>频率分辨率，单位 Hz。</summary>
    public double FrequencyResolution { get; init; }
}

/// <summary>
/// 频率响应函数（FRF）估计器，支持 H1、H2 估计方法。
/// </summary>
public static class TransferFunctionEstimator
{
    /// <summary>
    /// H1 估计：H1(f) = Gxy(f) / Gxx(f)，适用于输出噪声为主的情形。
    /// </summary>
    /// <param name="input">输入/激励信号（通道 x）。</param>
    /// <param name="output">输出/响应信号（通道 y），长度须与 <paramref name="input"/> 相同。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>频率响应函数 <see cref="TransferFunctionResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号长度不一致、为空或不足以分段时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static TransferFunctionResult EstimateH1(
        double[] input,
        double[] output,
        double sampleRate,
        int segmentLength = 1024,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning)
    {
        return Estimate(input, output, sampleRate, segmentLength, overlapRatio, windowType, useH2: false);
    }

    /// <summary>
    /// H2 估计：H2(f) = Gyy(f) / Gyx(f)，适用于输入噪声为主的情形。
    /// </summary>
    /// <param name="input">输入/激励信号（通道 x）。</param>
    /// <param name="output">输出/响应信号（通道 y），长度须与 <paramref name="input"/> 相同。</param>
    /// <param name="sampleRate">采样率，单位 Hz，须大于 0。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>频率响应函数 <see cref="TransferFunctionResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当信号长度不一致、为空或不足以分段时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当参数超出有效范围时抛出。</exception>
    public static TransferFunctionResult EstimateH2(
        double[] input,
        double[] output,
        double sampleRate,
        int segmentLength = 1024,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning)
    {
        return Estimate(input, output, sampleRate, segmentLength, overlapRatio, windowType, useH2: true);
    }

    private static TransferFunctionResult Estimate(
        double[] input,
        double[] output,
        double sampleRate,
        int segmentLength,
        double overlapRatio,
        WindowType windowType,
        bool useH2)
    {
        SignalGuard.SameLength(input, output);
        SignalGuard.Positive(sampleRate, nameof(sampleRate));
        SignalGuard.Positive(segmentLength, nameof(segmentLength));

        if (overlapRatio < 0 || overlapRatio >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapRatio), overlapRatio, "Overlap ratio must be in [0, 1).");
        }

        var window = WindowGenerator.Create(segmentLength, windowType);
        var hop = Math.Max(1, (int)Math.Round(segmentLength * (1.0 - overlapRatio)));
        var half = segmentLength / 2;
        var gxx = new Complex[half + 1];
        var gyy = new Complex[half + 1];
        var gxy = new Complex[half + 1];
        var segmentCount = 0;

        for (var start = 0; start + segmentLength <= input.Length; start += hop)
        {
            var segX = WindowedSegment(input, start, segmentLength, window);
            var segY = WindowedSegment(output, start, segmentLength, window);
            var specX = FourierTransform.Forward(segX);
            var specY = FourierTransform.Forward(segY);

            for (var i = 0; i <= half; i++)
            {
                gxx[i] += specX[i] * Complex.Conjugate(specX[i]);
                gyy[i] += specY[i] * Complex.Conjugate(specY[i]);
                gxy[i] += specX[i] * Complex.Conjugate(specY[i]);
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
            gxy[i] /= segmentCount;
        }

        var magnitude = new double[half + 1];
        var phase = new double[half + 1];
        var real = new double[half + 1];
        var imag = new double[half + 1];

        for (var i = 0; i <= half; i++)
        {
            Complex h;
            if (useH2)
            {
                h = gxy[i].Magnitude > 0 ? gyy[i] / Complex.Conjugate(gxy[i]) : Complex.Zero;
            }
            else
            {
                h = gxx[i].Magnitude > 0 ? gxy[i] / gxx[i] : Complex.Zero;
            }

            magnitude[i] = h.Magnitude;
            phase[i] = h.Phase;
            real[i] = h.Real;
            imag[i] = h.Imaginary;
        }

        return new TransferFunctionResult
        {
            Frequencies = FourierTransform.FrequencyAxis(segmentLength, sampleRate),
            Magnitude = magnitude,
            PhaseRadians = phase,
            Real = real,
            Imaginary = imag,
            FrequencyResolution = sampleRate / segmentLength
        };
    }

    private static double[] WindowedSegment(double[] data, int start, int length, double[] window)
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
