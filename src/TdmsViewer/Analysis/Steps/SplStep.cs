using NVHAlgorithmKit;
using NVHAlgorithmKit.Acoustics;
using NVHAlgorithmKit.Core;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class SplStep : IAnalysisStep
{
    public string StepType => "Spl";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Spl",
        DisplayName = "A 计权声压级",
        Description = "整体 SPL(A)，单位 dB",
        Category = "声学"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var spl = NvhAnalyzer.ComputeSpl(signal, WeightingType.A);

        var block = new ReportBlock
        {
            BlockId = "spl",
            WidgetType = "KpiCard",
            Title = "SPL (A)",
            Payload = new
            {
                label = "SPL (A)",
                value = spl,
                unit = "dB",
                format = "F1"
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
