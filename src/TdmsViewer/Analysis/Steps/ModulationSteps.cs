using NvhLibCSharp;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class ModulationSpectrumStep : IAnalysisStep
{
    public string StepType => "ModulationSpectrum";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "ModulationSpectrum",
        DisplayName = "调制谱",
        Description = "基于 Morlet 的调制谱分析",
        Category = "调制"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var frequencyResolution = StepParameters.GetDouble(parameters, "frequencyResolution", 1.0);
        var cutoffFreq = StepParameters.GetDouble(parameters, "cutoffFreq", 150.0);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 1.0 / 150);
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var scaleOpt = new ScaleOptions(scale, referenceValue);
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = NvhSignalAdapter.ToSignal(source.Samples, source.SampleRateHz);
            var spectrogram = Nvh.ModulationSpectrumAnalysis(
                signal,
                frequencyResolution,
                cutoffFreq,
                scaleOpt,
                out var freqAxis,
                out var timeAxis,
                out var modulationDepth,
                out _);

            cards.Add(NvhStepCharts.Heatmap(
                $"mod-{source.FilePath}",
                $"调制谱 · {source.Label}",
                "时间 (s)",
                "频率 (Hz)",
                spectrogram,
                timeAxis,
                freqAxis,
                source.FilePath));

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, modulationDepth);
            cards.Add(NvhStepCharts.Line(
                $"mod-depth-{source.FilePath}",
                $"调制深度 · {source.Label}",
                "时间 (s)",
                "调制深度",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class ModulationSpectrumStftStep : IAnalysisStep
{
    public string StepType => "ModulationSpectrumStft";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "ModulationSpectrumStft",
        DisplayName = "调制谱 (STFT)",
        Description = "基于 STFT 的调制谱分析",
        Category = "调制"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var windowSize = StepParameters.GetInt(parameters, "windowSize", 4096);
        var hopSize = StepParameters.GetInt(parameters, "hopSize", 1024);
        var cutoffFreq = StepParameters.GetDouble(parameters, "cutoffFreq", 150.0);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 1.0 / 150);
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var scaleOpt = new ScaleOptions(scale, referenceValue);
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = NvhSignalAdapter.ToSignal(source.Samples, source.SampleRateHz);
            var spectrogram = Nvh.ModulationSpectrumAnalysis(
                signal,
                windowSize,
                hopSize,
                cutoffFreq,
                scaleOpt,
                out var freqAxis,
                out var timeAxis,
                out var modulationDepth,
                out _);

            cards.Add(NvhStepCharts.Heatmap(
                $"modstft-{source.FilePath}",
                $"调制谱 STFT · {source.Label}",
                "时间 (s)",
                "频率 (Hz)",
                spectrogram,
                timeAxis,
                freqAxis,
                source.FilePath));

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, modulationDepth);
            cards.Add(NvhStepCharts.Line(
                $"modstft-depth-{source.FilePath}",
                $"调制深度 · {source.Label}",
                "时间 (s)",
                "调制深度",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}
