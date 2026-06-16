using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class WaveformStep : IAnalysisStep
{
    public string StepType => "Waveform";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Waveform",
        DisplayName = "时域波形",
        Description = "通道时域波形（降采样显示）",
        Category = "时域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var card = new LineChartModel(
            "wf",
            "时域波形",
            "时间 (s)",
            "幅值",
            AnalysisChartBuilder.BuildWaveformSeries(input.Sources));

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([card]);
    }
}
