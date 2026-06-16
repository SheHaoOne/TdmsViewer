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
        Description = "当前通道时域曲线（自动降采样）",
        Category = "时域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (xs, ys) = AnalysisPlotHelper.DownsampleSeries(input.Samples, input.SampleRateHz);

        var block = new ReportBlock
        {
            BlockId = "wf",
            WidgetType = "LineChart",
            Title = "时域波形",
            Payload = new
            {
                x = xs,
                y = ys,
                xLabel = "时间 (s)",
                yLabel = "幅值"
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
