using NVHAlgorithmKit;
using NVHAlgorithmKit.Acoustics;
using NVHAlgorithmKit.Core;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class TimeDomainStep : IAnalysisStep
{
    public string StepType => "TimeDomain";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "TimeDomain",
        DisplayName = "时域指标",
        Description = "RMS、峰值、波峰因子、峭度等",
        Category = "时域"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var features = NvhAnalyzer.AnalyzeTimeDomain(signal);

        var block = new ReportBlock
        {
            BlockId = "td",
            WidgetType = "KpiGroup",
            Title = "时域特征",
            Payload = new
            {
                items = new[]
                {
                    new { label = "RMS", value = features.Rms, unit = "", format = "G4" },
                    new { label = "峰值", value = features.Peak, unit = "", format = "G4" },
                    new { label = "峰峰值", value = features.PeakToPeak, unit = "", format = "G4" },
                    new { label = "波峰因子", value = features.CrestFactor, unit = "", format = "F2" },
                    new { label = "峭度", value = features.Kurtosis, unit = "", format = "F2" },
                    new { label = "偏度", value = features.Skewness, unit = "", format = "F2" }
                }
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
