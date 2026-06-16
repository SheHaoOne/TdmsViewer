using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.Windows;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class SpectrumStep : IAnalysisStep
{
    public string StepType => "Spectrum";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Spectrum",
        DisplayName = "频谱分析",
        Description = "Hanning 窗 FFT 幅值谱",
        Category = "频域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var spectrum = NvhAnalyzer.AnalyzeSpectrum(signal, WindowType.Hanning, removeDc: true);
        var (xs, ys) = AnalysisPlotHelper.DownsampleXY(spectrum.Frequencies, spectrum.Magnitude);

        var block = new ReportBlock
        {
            BlockId = "sp",
            WidgetType = "LineChart",
            Title = "频谱",
            Payload = new
            {
                x = xs,
                y = ys,
                xLabel = "频率 (Hz)",
                yLabel = "幅值"
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
