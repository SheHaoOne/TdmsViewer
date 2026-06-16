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
            ["TimeDomain"] = () => new TimeDomainStep(),
            ["Waveform"] = () => new WaveformStep(),
            ["Spectrum"] = () => new SpectrumStep(),
            ["OctaveBands"] = () => new OctaveBandsStep(),
            ["Spl"] = () => new SplStep(),
            ["Psd"] = () => new PsdStep(),
            ["Stft"] = () => new StftStep()
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
