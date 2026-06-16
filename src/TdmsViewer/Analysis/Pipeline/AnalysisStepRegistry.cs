using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Steps;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisStepRegistry
{
    private readonly Dictionary<string, Func<IAnalysisStep>> _factories;

    public AnalysisStepRegistry()
    {
        _factories = new Dictionary<string, Func<IAnalysisStep>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Waveform"] = () => new WaveformStep(),
            ["OverallLevel"] = () => new OverallLevelStep(),
            ["AveragedSpectrum"] = () => new AveragedSpectrumStep(),
            ["OctaveBands"] = () => new OctaveBandsStep(),
            ["TimeFrequencyMap"] = () => new TimeFrequencyMapStep(),
            ["HilbertEnvelope"] = () => new HilbertEnvelopeStep(),
            ["HilbertEnvelopeSpectra"] = () => new HilbertEnvelopeSpectraStep(),
            ["HilbertEnvelopeAvgSpectra"] = () => new HilbertEnvelopeAvgSpectraStep(),
            ["HilbertEnvelopeExFixed"] = () => new HilbertEnvelopeExFixedStep(),
            ["HilbertEnvelopeExTracked"] = () => new HilbertEnvelopeExTrackedStep(),
            ["MorletWavelet"] = () => new MorletWaveletStep(),
            ["LmsMorletWavelet"] = () => new LmsMorletWaveletStep(),
            ["ModulationSpectrum"] = () => new ModulationSpectrumStep(),
            ["ModulationSpectrumStft"] = () => new ModulationSpectrumStftStep(),
            ["StationaryLoudness"] = () => new StationaryLoudnessStep(),
            ["TimeVaryingLoudness"] = () => new TimeVaryingLoudnessStep(),
            ["StationarySharpness"] = () => new StationarySharpnessStep(),
            ["TimeVaryingSharpness"] = () => new TimeVaryingSharpnessStep(),
            ["Roughness"] = () => new RoughnessStep(),
            ["FluctuationStrength"] = () => new FluctuationStrengthStep(),
            ["Resample"] = () => new ResampleStep(),
            ["OrderSection"] = () => new OrderSectionStep(),
            ["RpmFrequencyMap"] = () => new RpmFrequencyMapStep(),
            ["RpmOrderMap"] = () => new RpmOrderMapStep()
        };
    }

    public IReadOnlyList<StepDefinition> GetDefinitions() =>
        _factories.Values
            .Select(factory => factory().Definition)
            .OrderBy(d => d.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IAnalysisStep Resolve(string stepType)
    {
        if (!_factories.TryGetValue(stepType, out var factory))
            throw new InvalidOperationException($"未注册的分析步骤：{stepType}");

        return factory();
    }
}
