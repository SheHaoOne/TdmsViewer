using NVHAlgorithmKit.Acoustics;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Fatigue;
using NVHAlgorithmKit.FrequencyDomain;
using NVHAlgorithmKit.Modal;
using NVHAlgorithmKit.Order;
using NVHAlgorithmKit.TimeDomain;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Vibration;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit;

/// <summary>
/// NVH 算法库统一入口，封装常用分析流程，简化调用。
/// </summary>
public static class NvhAnalyzer
{
    /// <summary>
    /// 计算时域特征指标（RMS、峰值、波峰因子、偏度、峭度等）。
    /// </summary>
    /// <param name="signal">输入 NVH 信号，包含采样数据与采样率。</param>
    /// <returns>时域特征集合 <see cref="TimeDomainFeatures"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static TimeDomainFeatures AnalyzeTimeDomain(NvhSignal signal) =>
        TimeDomainMetrics.ComputeAll(signal.Samples);

    /// <summary>
    /// 执行频谱分析（加窗 FFT）。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <param name="removeDc">是否去除直流分量，默认 <see langword="true"/>。</param>
    /// <returns>频谱分析结果 <see cref="SpectrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static SpectrumResult AnalyzeSpectrum(
        NvhSignal signal,
        WindowType windowType = WindowType.Hanning,
        bool removeDc = true) =>
        SpectrumAnalyzer.Analyze(signal.Samples, signal.SampleRate, windowType, removeDc);

    /// <summary>
    /// 使用 Welch 方法估计功率谱密度。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="segmentLength">每段 FFT 长度（采样点数），默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>PSD 估计结果 <see cref="PsdResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static PsdResult AnalyzePsd(
        NvhSignal signal,
        int segmentLength = 1024,
        double overlapRatio = 0.5,
        WindowType windowType = WindowType.Hanning) =>
        PowerSpectralDensity.EstimateWelch(
            signal.Samples,
            signal.SampleRate,
            segmentLength,
            overlapRatio,
            windowType);

    /// <summary>
    /// 执行短时傅里叶变换（STFT）时频分析。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="segmentLength">每帧 FFT 长度，默认 512。</param>
    /// <param name="overlapRatio">帧间重叠比例 [0, 1)，默认 0.75。</param>
    /// <param name="windowType">加窗类型，默认 Hanning 窗。</param>
    /// <returns>STFT 时频分析结果 <see cref="StftResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static StftResult AnalyzeStft(
        NvhSignal signal,
        int segmentLength = 512,
        double overlapRatio = 0.75,
        WindowType windowType = WindowType.Hanning) =>
        StftAnalyzer.Analyze(signal.Samples, signal.SampleRate, segmentLength, overlapRatio, windowType);

    /// <summary>
    /// 计算实倒谱，用于齿轮/轴承周期性故障检测。
    /// </summary>
    /// <param name="signal">输入振动或声学信号。</param>
    /// <returns>倒谱分析结果 <see cref="CepstrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static CepstrumResult AnalyzeCepstrum(NvhSignal signal) =>
        CepstrumAnalyzer.ComputeRealCepstrum(signal.Samples, signal.SampleRate);

    /// <summary>
    /// 估计双通道相干函数。
    /// </summary>
    /// <param name="input">输入/激励通道信号。</param>
    /// <param name="output">输出/响应通道信号，采样率须与 <paramref name="input"/> 相同。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <returns>相干函数结果 <see cref="CoherenceResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当信号为 <see langword="null"/> 时抛出。</exception>
    public static CoherenceResult AnalyzeCoherence(
        NvhSignal input,
        NvhSignal output,
        int segmentLength = 1024,
        double overlapRatio = 0.5) =>
        CoherenceAnalyzer.Estimate(input.Samples, output.Samples, input.SampleRate, segmentLength, overlapRatio);

    /// <summary>
    /// 估计 H1 频率响应函数（适用于输出噪声为主）。
    /// </summary>
    /// <param name="input">输入/激励通道信号。</param>
    /// <param name="output">输出/响应通道信号，采样率须与 <paramref name="input"/> 相同。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 1024。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <returns>频率响应函数 <see cref="TransferFunctionResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当信号为 <see langword="null"/> 时抛出。</exception>
    public static TransferFunctionResult AnalyzeTransferFunctionH1(
        NvhSignal input,
        NvhSignal output,
        int segmentLength = 1024,
        double overlapRatio = 0.5) =>
        TransferFunctionEstimator.EstimateH1(input.Samples, output.Samples, input.SampleRate, segmentLength, overlapRatio);

    /// <summary>
    /// 计算信号自相关函数。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="maxLag">最大滞后点数，默认 -1（使用全长 - 1）。</param>
    /// <returns>自相关数组，零滞后归一化为 1.0。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double[] AnalyzeAutoCorrelation(NvhSignal signal, int maxLag = -1) =>
        CorrelationAnalysis.AutoCorrelation(signal.Samples, maxLag);

    /// <summary>
    /// 执行倍频程分析（1/1 或 1/3 倍频程）。
    /// </summary>
    /// <param name="signal">输入声压信号，单位 Pa。</param>
    /// <param name="fraction">倍频程分频比例，默认 1/3 倍频程。</param>
    /// <param name="weighting">频率计权类型，默认 A 计权。</param>
    /// <returns>倍频程分析结果 <see cref="OctaveBandResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static OctaveBandResult AnalyzeOctaveBands(
        NvhSignal signal,
        OctaveFraction fraction = OctaveFraction.Third,
        WeightingType weighting = WeightingType.A) =>
        OctaveBandAnalyzer.Analyze(signal.Samples, signal.SampleRate, fraction, weighting);

    /// <summary>
    /// 计算计权声压级（SPL）。
    /// </summary>
    /// <param name="signal">输入声压信号，单位 Pa。</param>
    /// <param name="weighting">频率计权类型，默认 A 计权（返回 dB(A)）。</param>
    /// <returns>声压级，单位 dB。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double ComputeSpl(NvhSignal signal, WeightingType weighting = WeightingType.A) =>
        AcousticWeighting.ComputeWeightedLevel(signal.Samples, signal.SampleRate, weighting);

    /// <summary>
    /// 将加速度信号积分转换为速度信号。
    /// </summary>
    /// <param name="acceleration">输入加速度信号，单位 m/s²。</param>
    /// <param name="highPassCutoff">积分高通截止频率，单位 Hz，默认 0.5 Hz，用于抑制漂移。</param>
    /// <returns>速度信号 <see cref="NvhSignal"/>，单位 m/s，保持原采样率。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="acceleration"/> 为 <see langword="null"/> 时抛出。</exception>
    public static NvhSignal IntegrateToVelocity(NvhSignal acceleration, double highPassCutoff = 0.5) =>
        new(VibrationIntegrator.Integrate(acceleration.Samples, acceleration.SampleRate, highPassCutoff), acceleration.SampleRate);

    /// <summary>
    /// 将加速度信号二次积分转换为位移信号。
    /// </summary>
    /// <param name="acceleration">输入加速度信号，单位 m/s²。</param>
    /// <param name="highPassCutoff">积分高通截止频率，单位 Hz，默认 0.5 Hz。</param>
    /// <returns>位移信号 <see cref="NvhSignal"/>，单位 m，保持原采样率。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="acceleration"/> 为 <see langword="null"/> 时抛出。</exception>
    public static NvhSignal IntegrateToDisplacement(NvhSignal acceleration, double highPassCutoff = 0.5) =>
        new(VibrationIntegrator.IntegrateTwice(acceleration.Samples, acceleration.SampleRate, highPassCutoff), acceleration.SampleRate);

    /// <summary>
    /// 执行阶次谱分析，适用于旋转机械振动信号。
    /// </summary>
    /// <param name="signal">振动时域信号。</param>
    /// <param name="rpmTrace">与信号同步的转速轨迹，单位 RPM，长度须与信号相同。</param>
    /// <param name="orderCount">阶次轴分辨率（点数），默认 64。</param>
    /// <param name="maxOrder">最大分析阶次，默认 10.0。</param>
    /// <returns>阶次谱结果 <see cref="OrderSpectrumResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 或 <paramref name="rpmTrace"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当转速轨迹长度与信号不一致时抛出。</exception>
    public static OrderSpectrumResult AnalyzeOrders(NvhSignal signal, double[] rpmTrace, int orderCount = 64, double maxOrder = 10.0) =>
        OrderAnalyzer.ComputeOrderSpectrum(signal.Samples, signal.SampleRate, rpmTrace, orderCount, maxOrder);

    /// <summary>
    /// 生成 Campbell 图（转速-频率-幅值谱阵），用于扫频共振分析。
    /// </summary>
    /// <param name="signal">振动时域信号。</param>
    /// <param name="rpmTrace">同步转速轨迹，单位 RPM，长度须与信号相同。</param>
    /// <param name="segmentLength">每段 FFT 长度，默认 2048。</param>
    /// <param name="overlapRatio">段间重叠比例 [0, 1)，默认 0.5。</param>
    /// <returns>Campbell 图数据 <see cref="CampbellDiagramResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当信号或转速轨迹为 <see langword="null"/> 时抛出。</exception>
    public static CampbellDiagramResult AnalyzeCampbellDiagram(
        NvhSignal signal,
        double[] rpmTrace,
        int segmentLength = 2048,
        double overlapRatio = 0.5) =>
        CampbellDiagramAnalyzer.Compute(signal.Samples, signal.SampleRate, rpmTrace, segmentLength, overlapRatio);

    /// <summary>
    /// 执行 Morlet 连续小波变换（CWT）时频分析。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="minFrequency">最低分析频率，单位 Hz，默认 10 Hz。</param>
    /// <param name="maxFrequency">最高分析频率，单位 Hz，默认 0（自动取 Nyquist/4）。</param>
    /// <param name="frequencyCount">频率点数，默认 64。</param>
    /// <returns>CWT 分析结果 <see cref="CwtResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static CwtResult AnalyzeWavelet(
        NvhSignal signal,
        double minFrequency = 10,
        double maxFrequency = 0,
        int frequencyCount = 64) =>
        ContinuousWaveletTransform.AnalyzeMorlet(signal.Samples, signal.SampleRate, minFrequency, maxFrequency, frequencyCount);

    /// <summary>
    /// 从 FRF 或频谱中识别模态参数（固有频率、阻尼比）。
    /// </summary>
    /// <param name="spectrum">频谱或 FRF 幅值结果。</param>
    /// <param name="peakThreshold">峰值检测阈值（相对最大值比例），默认 0.1。</param>
    /// <param name="minPeakDistanceHz">相邻峰最小频率间隔，单位 Hz，默认 5 Hz。</param>
    /// <returns>模态识别结果 <see cref="ModalAnalysisResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="spectrum"/> 为 <see langword="null"/> 时抛出。</exception>
    public static ModalAnalysisResult IdentifyModalParameters(
        SpectrumResult spectrum,
        double peakThreshold = 0.1,
        double minPeakDistanceHz = 5.0) =>
        ModalParameterIdentifier.IdentifyFromSpectrum(spectrum, peakThreshold, minPeakDistanceHz);

    /// <summary>
    /// 计算声品质指标（尖锐度、粗糙度、波动度）。
    /// </summary>
    /// <param name="signal">输入声压信号，单位 Pa。</param>
    /// <returns>声品质指标 <see cref="SoundQualityResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static SoundQualityResult AnalyzeSoundQuality(NvhSignal signal) =>
        SoundQualityMetrics.ComputeAll(signal.Samples, signal.SampleRate);

    /// <summary>
    /// 计算 Zwicker 响度（sone）。
    /// </summary>
    /// <param name="signal">输入声压信号，单位 Pa。</param>
    /// <returns>响度分析结果 <see cref="LoudnessResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static LoudnessResult AnalyzeLoudness(NvhSignal signal) =>
        LoudnessAnalyzer.ComputeDetailed(signal.Samples, signal.SampleRate);

    /// <summary>
    /// 从键相脉冲信号提取 RPM 轨迹。
    /// </summary>
    /// <param name="tachometer">键相脉冲信号。</param>
    /// <param name="pulsesPerRevolution">每转脉冲数，默认 1。</param>
    /// <param name="smoothingWindow">RPM 平滑窗口（采样点数），默认 0。</param>
    /// <returns>转速提取结果 <see cref="RpmExtractionResult"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="tachometer"/> 为 <see langword="null"/> 时抛出。</exception>
    public static RpmExtractionResult ExtractRpmFromTachometer(
        NvhSignal tachometer,
        double pulsesPerRevolution = 1,
        int smoothingWindow = 0) =>
        RpmExtractor.FromTachometer(tachometer.Samples, tachometer.SampleRate, pulsesPerRevolution, smoothingWindow);

    /// <summary>
    /// 从振动信号估算主转速（1 倍转频法）。
    /// </summary>
    /// <param name="signal">振动时域信号。</param>
    /// <param name="minRpm">搜索最低转速，单位 RPM，默认 60。</param>
    /// <param name="maxRpm">搜索最高转速，单位 RPM，默认 10000。</param>
    /// <returns>估算转速，单位 RPM。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static double EstimateRpm(NvhSignal signal, double minRpm = 60, double maxRpm = 10000) =>
        RpmExtractor.EstimateFromVibration(signal.Samples, signal.SampleRate, minRpm, maxRpm);

    /// <summary>
    /// 对载荷时程执行雨流计数（疲劳分析）。
    /// </summary>
    /// <param name="signal">载荷或应力时域信号。</param>
    /// <returns>雨流循环列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IReadOnlyList<RainflowCycle> AnalyzeRainflow(NvhSignal signal) =>
        RainflowCounter.Count(signal.Samples);

    /// <summary>
    /// 将信号重采样到目标采样率。
    /// </summary>
    /// <param name="signal">输入 NVH 信号。</param>
    /// <param name="targetSampleRate">目标采样率，单位 Hz，须大于 0。</param>
    /// <returns>重采样后的新 <see cref="NvhSignal"/> 实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="signal"/> 为 <see langword="null"/> 时抛出。</exception>
    public static NvhSignal Resample(NvhSignal signal, double targetSampleRate) =>
        SignalResampler.Resample(signal, targetSampleRate);
}
