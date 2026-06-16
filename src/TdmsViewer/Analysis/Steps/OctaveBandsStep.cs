using NvhLibCSharp;
using NvhLibCSharp.Enums;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
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
        DisplayName = "1/3 倍频程",
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
        var octave = NvhEnumHelper.ParseOctave(StepParameters.GetString(parameters, "octave", "ThirdOctave"));

        var signal = NvhSignalAdapter.ToSignal(input.Samples, input.SampleRateHz);
        var calcOpt = new SpectraCalcOptions(SpectraCalcType.SpectrumLines, spectrumLines);
        var stepOpt = new SpectraStepOptions(SpectraStepType.Overlap, overlap);
        var linearScale = new ScaleOptions(Scale.Linear, 1);

        var spectra = Nvh.AveragedSpectrum(
            signal,
            calcOpt,
            stepOpt,
            linearScale,
            Format.Rms,
            Average.Energy,
            Window.Hanning,
            Weight.Linear);

        var frequencyStep = input.SampleRateHz / 2.0 / spectrumLines;
        var bandLevels = Nvh.Octave(
            spectra,
            frequencyStep,
            Window.Hanning,
            octave,
            new ScaleOptions(Scale.Db, referenceValue),
            out var bandCenters,
            out _,
            out _);

        var labels = bandCenters
            .Select(FormatFrequency)
            .ToArray();

        var card = new BarChartModel(
            "ob",
            "1/3 倍频程",
            "中心频率 (Hz)",
            "声压级 (dB)",
            bandCenters,
            bandLevels,
            labels);

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([card]);
    }

    private static string FormatFrequency(double hz) =>
        hz >= 1000 ? $"{hz / 1000:0.#}k" : hz.ToString("0");
}
