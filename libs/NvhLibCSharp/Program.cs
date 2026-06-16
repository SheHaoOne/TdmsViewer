// #define OVERALL
// #define ORDER_SECTION
// #define AVG_SPECTRA
// #define TIME_FREQ_MAP
// #define RPM_FREQ_MAP
// #define RPM_ORDER_MAP
// #define HILBERT
// #define HILBERT_EX
// #define MORLET_WAVELET
// #define MORLET_WAVELET_LMS
// #define MODULATION
// #define MODULATION_STFT
// #define STATIONARY_LOUDNESS
// #define TIME_VARYING_LOUDNESS
// #define STATIONARY_SHARPNESS
// #define TIME_VARYING_SHARPNESS
// #define ROUGHNESS
// #define OCTAVE
 #define RESAMPLE

using NvhLibCSharp.Interop;
using NvhLibCSharp.Enums;
using NvhLibCSharp.Options;
using NvhLibCSharp.Utils;

namespace NvhLibCSharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Nvh.LoadLicense("LIC-20260105-20bbbf6e.lic");
#if RESAMPLE
            ResampleDemo();
#endif

#if OVERALL
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var oaData = Nvh.OverallLevelSpectral(signal, 4096, 0.2, 2e-5, Window.Hanning, Weight.A, Scale.Linear, out var oaTimeAxis);

                for (int i = 0; i < oaTimeAxis.Length; i++)
                {
                    Console.WriteLine($"{oaTimeAxis[i]:F6}\t{oaData[i]:F6}");
                }
            }
#endif

#if ORDER_SECTION
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var rpmSample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\speed_0.txt");
                var rpm = new Rpm(rpmSample, 1.0 / 51200.0);

                var otsData = Nvh.OrderSection(signal, rpm, 4096, 14.0, 0.5, 1000, 4000, 25, 2e-5, Format.Rms, Window.Hanning, Weight.A, Scale.Linear, RpmTrigger.Up, out var otsRpmAxis);

                for (int i = 0; i < otsRpmAxis.Length; i++)
                {
                    Console.WriteLine($"{otsRpmAxis[i]:F6}\t{otsData[i]:F6}");
                }
            }
#endif

#if AVG_SPECTRA
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var calcOpt = new SpectraCalcOptions(calcType: Enums.SpectraCalcType.Resolution, calcValue: 1);
                var stepOpt = new SpectraStepOptions(stepType: Enums.SpectraStepType.Increment, stepValue: 0.15);
                var scaleOpt = new ScaleOptions(Scale.Db, 2e-5);

                var asData = Nvh.AveragedSpectrum(signal, calcOpt, stepOpt, scaleOpt, Format.Rms, Average.Energy, Window.Hanning, Weight.A);

                for (int i = 0; i < asData.Length; i++)
                {
                    Console.WriteLine($"{i}\t{asData[i]:F6}");
                }
            }
#endif

#if TIME_FREQ_MAP
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var tfmData = Nvh.TimeFrequencyMap(signal, 4096, 0.15, 2e-5, Format.Rms, Window.Hanning, Weight.A, Scale.Linear, out var tfmTimeAxis, out var tfmFreqAxis);

                for (int i = 0; i < tfmTimeAxis.Length; i++)
                {
                    for (int j = 0; j < tfmFreqAxis.Length; j++)
                    {
                        Console.WriteLine($"{tfmTimeAxis[i]:F6}\t{tfmFreqAxis[j]:F6}\t{tfmData[i, j]:F6}");
                    }
                }
            }
#endif

#if RPM_FREQ_MAP
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var rpmSample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\speed_0.txt");
                var rpm = new Rpm(rpmSample, 1.0 / 51200.0);

                var rfmData = Nvh.RpmFrequencyMap(signal, rpm, 4096, 1000, 4000, 25, 2e-5, Format.Rms, Window.Hanning, Weight.A, Scale.Linear, RpmTrigger.Up, out var rfmRpmAxis, out var rfmFreqAxis);

                for (int i = 0; i < sample.Length; i++)
                {
                    for (int j = 0; j < rfmFreqAxis.Length; j++)
                    {
                        Console.WriteLine($"{rfmRpmAxis[i]:F6}\t{rfmFreqAxis[j]:F6}\t{rfmData[i, j]:F6}");
                    }
                }
            }
#endif

#if RPM_ORDER_MAP
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);
                var rpmSample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\speed_0.txt");
                var rpm = new Rpm(rpmSample, 1.0 / 51200.0);

                var romData = Nvh.RpmOrderMap(signal, rpm, 32.0, 0.25, 600, 4000, 25, 2e-5, Format.Rms, Window.Hanning, Weight.A, Scale.Linear, out var romRpmAxis, out var romOrderAxis);
                for (int i = 0; i < romRpmAxis.Length; i++)
                {
                    for (int j = 0; j < romOrderAxis.Length; j++)
                    {
                        Console.WriteLine($"{romRpmAxis[i]:F6}\t{romOrderAxis[j]:F6}\t{romData[i, j]:F6}");
                    }
                }
            }
#endif

#if HILBERT
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var heData = Nvh.HilbertEnvelope(signal);
                var timeResolution = signal.DeltaTime;

                for (int i = 0; i < heData.Length; i++)
                {
                    Console.WriteLine($"{i * timeResolution:F6}\t{heData[i]:F6}");
                }
            }
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var heData = Nvh.HilbertEnvelopeSpectra(signal, Window.Hanning, Format.Rms, out var freq);

                for (int i = 0; i < heData.Length; i++)
                {
                    Console.WriteLine($"{freq[i]:F6}Hz\t{heData[i]:F6}");
                }
            }
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var calcOpt = new SpectraCalcOptions(Enums.SpectraCalcType.Resolution, 6.25);
                var stepOpt = new SpectraStepOptions(Enums.SpectraStepType.Overlap, 0.1);
                var heData = Nvh.HilbertEnvelopeAvgSpectra(signal, calcOpt, stepOpt, Format.Rms, Average.Energy, Window.Hanning, Weight.A, out var freq);

                for (int i = 0; i < heData.Length; i++)
                {
                    Console.WriteLine($"{freq[i]:F6}Hz\t{heData[i]:F6}");
                }
            }
#endif

#if MORLET_WAVELET
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200.0);

                var frequencyAxis = MathUtils.Logspace(Math.Log10(1.0), Math.Log10(51200.0 / 2), 50);
                var scaleOpt = new ScaleOptions(Scale.Db, 2e-5);

                var tf = Nvh.MorletWaveletTransform(signal, scaleOpt, [.. frequencyAxis], 5, out var timeAxis);

                for (int i = 0; i < tf.GetLength(1); i++)
                {
                    Console.WriteLine($"{timeAxis[i]:F6}\t{tf[0, i]:F6}");
                }
            }
#endif

#if MORLET_WAVELET_LMS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200.0);

                var scaleOpt = new ScaleOptions(Scale.Db, 2e-5);

                var tf = Nvh.LmsMorletWaveletTransform(signal, scaleOpt, 10, 1000, 100, out var timeAxis, out var freqAxis);

                for (int i = 0; i < tf.GetLength(1); i++)
                {
                    Console.WriteLine($"{timeAxis[i]:F6}\t{tf[0, i]:F6}");
                }
            }
#endif

#if MODULATION
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\chirp.txt");
                var signal = new Signal(sample, 1.0 / 51200.0);

                var scaleOpt = new ScaleOptions(Scale.Db, 1.0 / 150);
                var spectrogram = Nvh.ModulationSpectrumAnalysis(signal, 1.0, 150.0, scaleOpt, out var freqAxis, out var timeAxis, out var modulationDepth, out var modulationFreq);

                PlotHelper.PlotColormap("figures/modulation_spectrogram_morlet.png", spectrogram, timeAxis, freqAxis, 0, 40);
            }
#endif

#if MODULATION_STFT
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\chirp.txt");
                var signal = new Signal(sample, 1.0 / 51200.0);
                var scaleOpt = new ScaleOptions(Scale.Db, 1.0 / 150);

                var tf = Nvh.ModulationSpectrumAnalysis(signal, 51200, 5120, 150.0, scaleOpt, out var freqAxis, out var timeAxis, out var modDep, out var modFreq);

                PlotHelper.PlotColormap("figures/modulation_spectrogram_stft.png", tf, timeAxis, freqAxis, 0, 40);
            }
#endif

#if STATIONARY_LOUDNESS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var (loudness, specLoudness) = Nvh.StationaryLoudnessAnalyze(signal, Enums.SoundField.Free, 0.0, out var barks);
                Console.WriteLine($"Loudness: {loudness:F6} Sones");
                for (int i = 0; i < barks.Length; i++)
                {
                    Console.WriteLine($"{barks[i]:F6}\t{specLoudness[i]:F6}");
                }
            }
#endif

#if TIME_VARYING_LOUDNESS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var (loudness, specLoudness) = Nvh.TimeVaryingLoudnessAnalyze(signal, Enums.SoundField.Free, 0.0, out var barks, out var times);
                for (int i = 0; i < times.Length; i++)
                {
                    Console.WriteLine($"{times[i]:F6}\t{loudness[i]:F6}");
                }

                for (int i = 0; i < barks.Length; i++)
                {
                    Console.WriteLine($"{barks[i]:F6}\t{specLoudness[i, 0]:F6}");
                }
            }
#endif

#if STATIONARY_SHARPNESS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200);

                var sharpness = Nvh.StationarySharpnessAnalyze(signal, Enums.SharpnessWeighting.Din, Enums.SoundField.Free, 0.0, out var specSharpness, out var barkAxis);
                Console.WriteLine($"Sharpness: {sharpness:F6} acum");
            }
#endif

#if TIME_VARYING_SHARPNESS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200);
                var sharpness = Nvh.TimeVaryingSharpnessAnalyze(signal, Enums.SharpnessWeighting.Din, Enums.SoundField.Free, 0.0, out var specSharpness, out var barks, out var freqAxis, out var times);
                for (int i = 0; i < times.Length; i++)
                {
                    Console.WriteLine($"{times[i]:F6}\t{sharpness[i]:F6}");
                }
            }
#endif

#if ROUGHNESS
            {
                var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(sample, 1.0 / 51200);
                var roughness = Nvh.RoughnessAnalyze(signal, Enums.SoundField.Free, 0.3, out var timeVaringRoughness, out var specificRoughness, out var stationaryRoughness, out var bandAxis, out var barkAxis, out var timeAxis);
                Console.WriteLine($"Roughness: {roughness:F6} Asper");
                for (int i = 0; i < bandAxis.Length; i++)
                {
                    for (int j = 0; j < timeAxis.Length; j++)
                    {
                        Console.Write($"{specificRoughness[i, j]:F6}\t");
                    }
                    Console.WriteLine();
                }
                for (int i = 0; i < timeAxis.Length; i++)
                {
                    Console.WriteLine($"{timeAxis[i]:F6}\t{timeVaringRoughness[i]:F6}");
                }
                for (int i = 0; i < bandAxis.Length; i++)
                {
                    Console.WriteLine($"{barkAxis[i]:F6}\t{stationaryRoughness[i]:F6}");
                }
            }
#endif

#if HILBERT_EX
            {
                var samples = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\sound_signal_0.txt");
                var rpm = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\speed_0.txt");
                var signal = new Signal(samples, 1.0 / 51200);

                var fixedOptions = new EnvelopeExOptions(700, 1050);
                var fixedEnvlope = Nvh.HilbertEnvelopeEx(signal, fixedOptions);

                var trackedOptions = new EnvelopeExOptions(2.0, 1000, 4096, 100, 4000, rpm);
                var trackedEnvlope = Nvh.HilbertEnvelopeEx(signal, trackedOptions);

                var timeVector = Enumerable.Range(0, fixedEnvlope.Length).Select(i => i * signal.DeltaTime).ToArray();
                PlotHelper.PlotFigure("figures/hilbert_envelope_ex_fixed.png", timeVector, [(fixedEnvlope, "Fixed Bandwidth Envelope")]);
                PlotHelper.PlotFigure("figures/hilbert_envelope_ex_tracked.png", timeVector, [(trackedEnvlope, "Tracked Bandwidth Envelope")]);
            }
#endif

#if OCTAVE
            {
                var samples = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\channel_1.txt");
                var signal = new Signal(samples, 1.0 / 51200);
                var spectraOpt = new SpectraCalcOptions(Enums.SpectraCalcType.SpectrumLines, 4096);
                var stepOpt = new SpectraStepOptions(Enums.SpectraStepType.Overlap, 0.5);
                var scaleOpt = new ScaleOptions(Scale.Linear, 1);
                var spectra = Nvh.AveragedSpectrum(signal, spectraOpt, stepOpt, scaleOpt, Format.Rms, Average.Energy, Window.Hanning, Weight.Linear);
                var deltaF = 25600 / 2 / 4096;

                var bandLevels = Nvh.Octave(spectra, deltaF, Window.Hanning, Enums.Octave.ThirdOctave, new ScaleOptions(Scale.Db, 1.0), out var bandCenter, out _, out _);

                PlotHelper.PlotFigure("figures/octave_spectrum.png", bandCenter, [(bandLevels, "Third Octave Band Levels")]);
            }
#endif
        }

        private static void ResampleDemo()
        {
            const double sourceSamplerate = 51200.0;
            const double destSamplerate = 1800.0;
            const double bandRatio = 0.8;

            var sample = LoadData.Double("D:\\source\\NvhLibCSharp\\SampleData\\simple.txt");
            var signal = new Signal(sample, 1.0 / sourceSamplerate);
            var resampled = Nvh.ResampleSignal(signal, destSamplerate, bandRatio, FractionalResamplerPlanningMode.Balanced);

            Console.WriteLine($"Source: {sample.Length} samples @ {sourceSamplerate:F0} Hz");
            Console.WriteLine($"Resampled: {resampled.Length} samples @ {destSamplerate:F0} Hz");

            for (int i = 0; i < Math.Min(resampled.Length, 10); i++)
            {
                Console.WriteLine($"{i / destSamplerate:F6}\t{resampled[i]:F6}");
            }
        }
    }
}
