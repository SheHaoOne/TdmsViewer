using NvhLibCSharp;
using NvhLibCSharp.Interop;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class OverallLevelStep : IAnalysisStep
{
    public string StepType => "OverallLevel";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "OverallLevel",
        DisplayName = "总声级曲线",
        Description = "Overall Level Spectral（A 计权）",
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
        var increment = StepParameters.GetDouble(parameters, "increment", 0.2);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A"));
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));

        var signal = NvhSignalAdapter.ToSignal(input.Samples, input.SampleRateHz);
        var values = Nvh.OverallLevelSpectral(
            signal,
            spectrumLines,
            increment,
            referenceValue,
            window,
            weight,
            scale,
            out var timeAxis);

        var (xs, ys) = PlotDataHelper.DownsampleXY(timeAxis, values);
        var card = new LineChartModel(
            "spl",
            "总声级曲线",
            "时间 (s)",
            scale == Scale.Db ? "声压级 (dB)" : "幅值",
            [new LineSeriesData { Label = "Overall Level", X = xs, Y = ys, Color = "#FF9500" }]);

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([card]);
    }
}
