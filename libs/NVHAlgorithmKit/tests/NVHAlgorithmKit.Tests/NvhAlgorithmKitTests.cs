using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.FrequencyDomain;
using NVHAlgorithmKit.Order;
using NVHAlgorithmKit.TimeDomain;
using NVHAlgorithmKit.Transform;
using NVHAlgorithmKit.Vibration;
using NVHAlgorithmKit.Windows;

namespace NVHAlgorithmKit.Tests;

public class FourierTransformTests
{
    [Fact]
    public void Forward_Backward_RoundTrip_PreservesSignal()
    {
        const double sampleRate = 1000;
        const double frequency = 50;
        const int length = 1024;
        var signal = GenerateSine(length, sampleRate, frequency);

        var spectrum = FourierTransform.Forward(signal);
        var reconstructed = FourierTransform.Inverse(spectrum);

        for (var i = 0; i < signal.Length; i++)
        {
            Assert.Equal(signal[i], reconstructed[i], precision: 10);
        }
    }

    [Fact]
    public void RealForwardMagnitude_DetectsTone()
    {
        const double sampleRate = 48000;
        const double frequency = 1000;
        var signal = GenerateSine(4096, sampleRate, frequency);
        var magnitudes = FourierTransform.RealForwardMagnitude(signal);
        var frequencies = FourierTransform.FrequencyAxis(magnitudes.Length * 2 - 2, sampleRate);

        var peakIndex = Array.IndexOf(magnitudes, magnitudes.Max());
        Assert.InRange(frequencies[peakIndex], frequency - 20, frequency + 20);
    }

    private static double[] GenerateSine(int length, double sampleRate, double frequency)
    {
        var signal = new double[length];
        for (var i = 0; i < length; i++)
        {
            signal[i] = Math.Sin(2.0 * Math.PI * frequency * i / sampleRate);
        }

        return signal;
    }
}

public class TimeDomainMetricsTests
{
    [Fact]
    public void Rms_UnitSine_IsExpected()
    {
        var sine = Enumerable.Range(0, 1000)
            .Select(i => Math.Sin(2.0 * Math.PI * i / 1000))
            .ToArray();

        var rms = TimeDomainMetrics.Rms(sine);
        Assert.Equal(1.0 / Math.Sqrt(2), rms, precision: 3);
    }

    [Fact]
    public void CrestFactor_ConstantSignal_IsOne()
    {
        var signal = Enumerable.Repeat(2.0, 256).ToArray();
        Assert.Equal(1.0, TimeDomainMetrics.CrestFactor(signal), precision: 6);
    }
}

public class SpectrumAnalyzerTests
{
    [Fact]
    public void Analyze_FindsDominantFrequency()
    {
        const double sampleRate = 8192;
        const double frequency = 440;
        var samples = Enumerable.Range(0, 8192)
            .Select(i => Math.Sin(2.0 * Math.PI * frequency * i / sampleRate))
            .ToArray();

        var signal = new NvhSignal(samples, sampleRate);
        var spectrum = NvhAnalyzer.AnalyzeSpectrum(signal);
        var dominant = SpectrumAnalyzer.FindDominantFrequency(spectrum, 20, 2000);

        Assert.Equal(frequency, dominant, precision: 0);
    }
}

public class FilterTests
{
    [Fact]
    public void HighPassFilter_AttenuatesLowFrequencyTone()
    {
        const double sampleRate = 1000;
        const double lowFrequency = 1;
        var signal = Enumerable.Range(0, 4000)
            .Select(i => Math.Sin(2.0 * Math.PI * lowFrequency * i / sampleRate))
            .ToArray();

        var filter = Filtering.ButterworthDesigner.CreateHighPass(4, sampleRate, 20);
        var filtered = filter.ProcessBlock(signal);

        Assert.True(TimeDomainMetrics.Rms(filtered) < TimeDomainMetrics.Rms(signal) * 0.2);
    }
}

public class VibrationTests
{
    [Fact]
    public void Integrate_SineAcceleration_ProducesCosineVelocity()
    {
        const double sampleRate = 1000;
        const double frequency = 5;
        var acceleration = Enumerable.Range(0, 4000)
            .Select(i => Math.Sin(2.0 * Math.PI * frequency * i / sampleRate))
            .ToArray();

        var velocity = VibrationIntegrator.Integrate(acceleration, sampleRate, highPassCutoff: 0.5);
        var velocityRms = TimeDomainMetrics.Rms(velocity);

        Assert.True(velocityRms > 0.01);
    }
}

public class OrderAnalyzerTests
{
    [Fact]
    public void FrequencyToOrder_ConvertsCorrectly()
    {
        Assert.Equal(2.0, OrderAnalyzer.FrequencyToOrder(100, 3000), precision: 6);
    }
}

public class StftAnalyzerTests
{
    [Fact]
    public void Analyze_ProducesTimeFrequencyMatrix()
    {
        const double sampleRate = 1000;
        const int length = 4096;
        var samples = Enumerable.Range(0, length)
            .Select(i => Math.Sin(2.0 * Math.PI * 100 * i / sampleRate))
            .ToArray();

        var signal = new NvhSignal(samples, sampleRate);
        var stft = NvhAnalyzer.AnalyzeStft(signal, segmentLength: 256, overlapRatio: 0.5);

        Assert.True(stft.FrameCount > 0);
        Assert.Equal(stft.FrameCount, stft.TimeAxis.Length);
        Assert.True(stft.Magnitude.GetLength(0) == stft.FrameCount);
        Assert.True(stft.Magnitude.GetLength(1) == stft.Frequencies.Length);
    }
}

public class CorrelationAnalysisTests
{
    [Fact]
    public void AutoCorrelation_ZeroLag_IsOne()
    {
        var data = Enumerable.Range(0, 500)
            .Select(i => Math.Sin(2.0 * Math.PI * 10 * i / 1000))
            .ToArray();

        var acf = CorrelationAnalysis.AutoCorrelation(data, maxLag: 100);
        Assert.Equal(1.0, acf[100], precision: 3);
    }

    [Fact]
    public void CrossCorrelation_IdenticalSignals_PeakAtZeroLag()
    {
        var random = new Random(42);
        var x = Enumerable.Range(0, 500).Select(_ => random.NextDouble() * 2 - 1).ToArray();

        var corr = CorrelationAnalysis.CrossCorrelation(x, x, maxLag: 50);
        var peakIndex = Array.IndexOf(corr, corr.Max());

        Assert.Equal(50, peakIndex);
    }
}

public class CepstrumAnalyzerTests
{
    [Fact]
    public void ComputeRealCepstrum_ReturnsValidResult()
    {
        const double sampleRate = 8000;
        var samples = Enumerable.Range(0, 2048)
            .Select(i => Math.Sin(2.0 * Math.PI * 200 * i / sampleRate))
            .ToArray();

        var result = CepstrumAnalyzer.ComputeRealCepstrum(samples, sampleRate);
        Assert.Equal(samples.Length, result.RealCepstrum.Length);
        Assert.True(result.Quefrency[^1] > 0);
    }
}

public class CoherenceAnalyzerTests
{
    [Fact]
    public void Estimate_IdenticalSignals_CoherenceNearOne()
    {
        const double sampleRate = 1000;
        var samples = Enumerable.Range(0, 4096)
            .Select(i => Math.Sin(2.0 * Math.PI * 50 * i / sampleRate))
            .ToArray();

        var result = CoherenceAnalyzer.Estimate(samples, samples, sampleRate, segmentLength: 512);
        var midIndex = result.Coherence.Length / 4;
        Assert.True(result.Coherence[midIndex] > 0.9);
    }
}

public class TransferFunctionEstimatorTests
{
    [Fact]
    public void EstimateH1_LinearSystem_ReturnsExpectedGain()
    {
        const double sampleRate = 1000;
        const double gain = 2.5;
        var input = Enumerable.Range(0, 4096)
            .Select(i => Math.Sin(2.0 * Math.PI * 50 * i / sampleRate))
            .ToArray();
        var output = input.Select(v => v * gain).ToArray();

        var frf = TransferFunctionEstimator.EstimateH1(input, output, sampleRate, segmentLength: 512);
        var midIndex = frf.Magnitude.Length / 4;
        Assert.InRange(frf.Magnitude[midIndex], gain * 0.5, gain * 1.5);
    }
}

public class CampbellDiagramTests
{
    [Fact]
    public void Compute_GeneratesRpmFrequencyMatrix()
    {
        const double sampleRate = 2000;
        const int length = 8192;
        var rpmTrace = Enumerable.Range(0, length).Select(i => 1000.0 + i * 3000.0 / length).ToArray();
        var signal = Enumerable.Range(0, length)
            .Select(i => Math.Sin(2.0 * Math.PI * 50 * i / sampleRate))
            .ToArray();

        var result = Order.CampbellDiagramAnalyzer.Compute(signal, sampleRate, rpmTrace, segmentLength: 1024);
        Assert.True(result.SegmentCount > 0);
        Assert.Equal(result.SegmentCount, result.RpmAxis.Length);
    }
}

public class CwtTests
{
    [Fact]
    public void AnalyzeMorlet_ProducesTimeFrequencyData()
    {
        const double sampleRate = 1000;
        var samples = Enumerable.Range(0, 1024)
            .Select(i => Math.Sin(2.0 * Math.PI * 100 * i / sampleRate))
            .ToArray();

        var result = Transform.ContinuousWaveletTransform.AnalyzeMorlet(samples, sampleRate, frequencyCount: 32);
        Assert.Equal(32, result.Frequencies.Length);
        Assert.Equal(1024, result.TimeAxis.Length);
        Assert.Equal(32, result.Magnitude.GetLength(0));
    }
}

public class ModalAnalysisTests
{
    [Fact]
    public void IdentifyFromSpectrum_FindsPeak()
    {
        const double sampleRate = 1000;
        var samples = Enumerable.Range(0, 2048)
            .Select(i => Math.Sin(2.0 * Math.PI * 100 * i / sampleRate))
            .ToArray();

        var spectrum = SpectrumAnalyzer.Analyze(samples, sampleRate);
        var modal = Modal.ModalParameterIdentifier.IdentifyFromSpectrum(spectrum, peakThreshold: 0.05);

        Assert.NotEmpty(modal.Peaks);
        Assert.InRange(modal.Peaks[0].NaturalFrequency, 90, 110);
    }
}

public class SoundQualityTests
{
    [Fact]
    public void ComputeAll_ReturnsNonNegativeMetrics()
    {
        const double sampleRate = 48000;
        var samples = Enumerable.Range(0, 48000)
            .Select(i => 0.1 * Math.Sin(2.0 * Math.PI * 1000 * i / sampleRate))
            .ToArray();

        var result = Acoustics.SoundQualityMetrics.ComputeAll(samples, sampleRate);
        Assert.True(result.Sharpness >= 0);
        Assert.True(result.Roughness >= 0);
        Assert.True(result.FluctuationStrength >= 0);
    }
}

public class RpmExtractorTests
{
    [Fact]
    public void FromTachometer_DetectsCorrectRpm()
    {
        const double sampleRate = 10000;
        const double rpm = 3000;
        const double freq = rpm / 60.0;
        var tach = Enumerable.Range(0, 10000)
            .Select(i => Math.Sin(2.0 * Math.PI * freq * i / sampleRate) > 0 ? 1.0 : 0.0)
            .ToArray();

        var result = Order.RpmExtractor.FromTachometer(tach, sampleRate);
        Assert.InRange(result.MeanRpm, rpm * 0.9, rpm * 1.1);
    }

    [Fact]
    public void EstimateFromVibration_FindsRpm()
    {
        const double sampleRate = 2000;
        const double rpm = 1200;
        var vibration = Enumerable.Range(0, 8000)
            .Select(i => Math.Sin(2.0 * Math.PI * (rpm / 60.0) * i / sampleRate))
            .ToArray();

        var estimated = Order.RpmExtractor.EstimateFromVibration(vibration, sampleRate, minRpm: 500, maxRpm: 2000);
        Assert.InRange(estimated, rpm * 0.8, rpm * 1.2);
    }
}

public class LoudnessAnalyzerTests
{
    [Fact]
    public void ComputeLoudness_ReturnsPositiveForTone()
    {
        const double sampleRate = 48000;
        var samples = Enumerable.Range(0, 48000)
            .Select(i => 0.1 * Math.Sin(2.0 * Math.PI * 1000 * i / sampleRate))
            .ToArray();

        var loudness = Acoustics.LoudnessAnalyzer.ComputeLoudness(samples, sampleRate);
        Assert.True(loudness > 0);
    }
}

public class RainflowCounterTests
{
    [Fact]
    public void Count_StandardLoad_ProducesCycles()
    {
        var load = new[] { 0.0, 2.0, 0.0, 2.0, 0.0, 2.0, 0.0 };
        var cycles = Fatigue.RainflowCounter.Count(load);
        Assert.NotEmpty(cycles);
        Assert.True(cycles.Sum(c => c.Count) > 0);
    }

    [Fact]
    public void BuildHistogram_AggregatesCycles()
    {
        var load = new[] { 0.0, 3.0, 0.0, 3.0, 0.0, 3.0, 0.0 };
        var cycles = Fatigue.RainflowCounter.Count(load);
        var histogram = Fatigue.RainflowCounter.BuildHistogram(cycles, binCount: 5);
        Assert.Equal(5, histogram.RangeBins.Length);
        Assert.True(histogram.TotalCycles > 0);
    }
}

public class SignalResamplerTests
{
    [Fact]
    public void Resample_ChangesLengthProportionally()
    {
        var data = Enumerable.Range(0, 1000).Select(i => (double)i).ToArray();
        var resampled = Core.SignalResampler.Resample(data, 48000, 16000);
        Assert.InRange(resampled.Length, 330, 334);
    }

    [Fact]
    public void Resample_SameRate_ReturnsClone()
    {
        var data = new[] { 1.0, 2.0, 3.0 };
        var resampled = Core.SignalResampler.Resample(data, 1000, 1000);
        Assert.Equal(data, resampled);
    }
}

public class MathUtilitiesTests
{
    [Fact]
    public void NextPowerOfTwo_ReturnsExpectedValues()
    {
        Assert.Equal(1024, MathUtilities.NextPowerOfTwo(1000));
        Assert.Equal(512, MathUtilities.NextPowerOfTwo(512));
    }

    [Fact]
    public void ToDecibels_AndFromDecibels_AreInverse()
    {
        var linear = 0.5;
        var db = MathUtilities.ToDecibels(linear);
        Assert.Equal(linear, MathUtilities.FromDecibels(db), precision: 10);
    }
}
