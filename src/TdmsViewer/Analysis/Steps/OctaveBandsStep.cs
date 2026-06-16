using NVHAlgorithmKit;
using NVHAlgorithmKit.Acoustics;
using NVHAlgorithmKit.Core;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

public sealed class OctaveBandsStep : IAnalysisStep
{
    public string StepType => "OctaveBands";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "OctaveBands",
        DisplayName = "1/3 倍频程",
        Description = "IEC 61260 倍频程（A 计权）",
        Category = "声学"
    };

    public Task<IReadOnlyList<ReportBlock>> ExecuteAsync(AnalysisInputContext input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signal = new NvhSignal(input.Samples, input.SampleRateHz);
        var octave = NvhAnalyzer.AnalyzeOctaveBands(signal, OctaveFraction.Third, WeightingType.A);

        var categories = octave.CenterFrequencies
            .Select(f => f >= 1000 ? $"{f / 1000:0.#}k" : f.ToString("0"))
            .ToArray();

        var values = octave.Levels
            .Select(level => double.IsNegativeInfinity(level) ? double.NaN : level)
            .ToArray();

        var block = new ReportBlock
        {
            BlockId = "ob",
            WidgetType = "BarChart",
            Title = "1/3 倍频程 (dB)",
            Payload = new
            {
                categories,
                values,
                yLabel = "声压级 (dB)"
            }
        };

        return Task.FromResult<IReadOnlyList<ReportBlock>>([block]);
    }
}
