using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Windows;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class PsdStep : IAnalysisStep
{
    public string StepType => "Psd";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Psd",
        DisplayName = "功率谱密度",
        Description = "Welch 方法 PSD 估计",
        Category = "频域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var psd = NvhAnalyzer.AnalyzePsd(signal, segmentLength: 1024, overlapRatio: 0.5, windowType: WindowType.Hanning);
        var (xs, ys) = AnalysisPlotHelper.DownsampleXY(psd.Frequencies, psd.Density);

        var block = new ReportBlock
        {
            BlockId = "psd",
            WidgetType = "LineChart",
            Title = "功率谱密度 (Welch)",
            Payload = new
            {
                x = xs,
                y = ys,
                xLabel = "频率 (Hz)",
                yLabel = "PSD"
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
