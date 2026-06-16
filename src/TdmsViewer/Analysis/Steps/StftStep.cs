using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Windows;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class StftStep : IAnalysisStep
{
    public string StepType => "Stft";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Stft",
        DisplayName = "STFT 时频图",
        Description = "短时傅里叶变换时频热力图",
        Category = "频域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var stft = NvhAnalyzer.AnalyzeStft(signal, segmentLength: 512, overlapRatio: 0.75, windowType: WindowType.Hanning);
        var (timeLabels, freqLabels, heatmapData) = AnalysisPlotHelper.BuildStftHeatmap(stft);

        var block = new ReportBlock
        {
            BlockId = "stft",
            WidgetType = "Heatmap",
            Title = "STFT 时频图",
            Payload = new
            {
                timeLabels,
                freqLabels,
                data = heatmapData
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
