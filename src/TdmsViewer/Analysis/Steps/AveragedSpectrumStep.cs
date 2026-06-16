using NvhLibCSharp;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class AveragedSpectrumStep : IAnalysisStep
{
    public string StepType => "AveragedSpectrum";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "AveragedSpectrum",
        DisplayName = "平均频谱",
        Description = "平均自功率谱（A 计权）",
        Category = "频域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var calcType = NvhEnumHelper.ParseCalcType(StepParameters.GetString(parameters, "calcType", "Resolution"));
        var calcValue = StepParameters.GetDouble(parameters, "calcValue", 1.0);
        var stepType = NvhEnumHelper.ParseStepType(StepParameters.GetString(parameters, "stepType", "Increment"));
        var stepValue = StepParameters.GetDouble(parameters, "stepValue", 0.15);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var average = NvhEnumHelper.ParseAverage(StepParameters.GetString(parameters, "average", "Energy"));
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A"));
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));

        var calcOpt = new SpectraCalcOptions(calcType, calcValue);
        var stepOpt = new SpectraStepOptions(stepType, stepValue);
        var scaleOpt = new ScaleOptions(scale, referenceValue);

        var series = new List<LineSeriesData>(input.Sources.Count);
        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var signal = NvhSignalAdapter.ToSignal(source.Samples, source.SampleRateHz);
            var values = Nvh.AveragedSpectrum(signal, calcOpt, stepOpt, scaleOpt, format, average, window, weight);
            var spectrumLines = PlotDataHelper.ResolveSpectrumLines(
                signal.DeltaTime,
                calcType.ToString(),
                calcValue);
            var freqAxis = PlotDataHelper.BuildFrequencyAxis(values.Length, signal.DeltaTime, spectrumLines);
            var (xs, ys) = PlotDataHelper.DownsampleXY(freqAxis, values);

            series.Add(new LineSeriesData
            {
                Label = source.Label,
                X = xs,
                Y = ys,
                Color = source.Color
            });
        }

        var yLabel = scale == Scale.Db ? "声压级 (dB)" : "幅值";
        var card = new LineChartModel(
            "as",
            "平均频谱",
            "频率 (Hz)",
            yLabel,
            series);

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([card]);
    }
}
