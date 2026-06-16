using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class WaveletStep : IAnalysisStep
{
    public string StepType => "Wavelet";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Wavelet",
        DisplayName = "小波变换 (CWT)",
        Description = "Morlet 连续小波变换时频热力图",
        Category = "频域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var cwt = NvhAnalyzer.AnalyzeWavelet(signal, minFrequency: 10, maxFrequency: 0, frequencyCount: 64);
        var (timeLabels, freqLabels, heatmapData) = AnalysisPlotHelper.BuildCwtHeatmap(cwt);

        var block = new ReportBlock
        {
            BlockId = "cwt",
            WidgetType = "Heatmap",
            Title = "小波变换 (CWT)",
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
