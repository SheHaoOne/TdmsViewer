using NvhLibCSharp;
using NvhLibCSharp.Enums;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Parameters;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class OctaveBandsStep : IAnalysisStep
{
    public string StepType => "OctaveBands";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "OctaveBands",
        DisplayName = "倍频程",
        Description = "基于平均谱的倍频程分析",
        Category = "声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var spectrumLines = StepParameters.GetInt(parameters, "spectrumLines", 4096);
        var overlap = StepParameters.GetDouble(parameters, "overlap", 0.5);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var octaveValue = StepParameters.GetString(parameters, "octave", "ThirdOctave");
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var average = NvhEnumHelper.ParseAverage(StepParameters.GetString(parameters, "average", "Energy"));
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "Linear"));
        var octave = NvhEnumHelper.ParseOctave(octaveValue);
        var chartTitle = AnalysisStepParameterCatalog.GetChoiceLabel("OctaveBands", "octave", octaveValue) ?? "倍频程";

        var calcOpt = new SpectraCalcOptions(SpectraCalcType.SpectrumLines, spectrumLines);
        var stepOpt = new SpectraStepOptions(SpectraStepType.Overlap, overlap);
        var linearScale = new ScaleOptions(Scale.Linear, 1);
        var dbScale = new ScaleOptions(Scale.Db, referenceValue);

        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var signal = NvhSignalAdapter.ToSignal(source.Samples, source.SampleRateHz);
            var spectra = Nvh.AveragedSpectrum(
                signal,
                calcOpt,
                stepOpt,
                linearScale,
                format,
                average,
                window,
                weight);

            var frequencyStep = source.SampleRateHz / 2.0 / spectrumLines;
            var bandLevels = Nvh.Octave(
                spectra,
                frequencyStep,
                window,
                octave,
                dbScale,
                out var centers,
                out _,
                out _);

            series.Add(new LineSeriesData
            {
                Label = source.Label,
                X = centers,
                Y = bandLevels,
                Color = source.Color,
                SourceKey = source.FilePath
            });
        }

        if (series.Count == 0)
            return Task.FromResult<IReadOnlyList<ChartCardModel>>([]);

        var card = new LineChartModel(
            "ob",
            chartTitle,
            "中心频率（Hz）",
            "声压级 (dB)",
            series,
            UseLogXAxis: true,
            UseStepLine: true);

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([card]);
    }
}
