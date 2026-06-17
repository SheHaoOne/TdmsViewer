using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class PipelineProgress
{
    public required int CompletedSteps { get; init; }
    public required int TotalSteps { get; init; }
    public required string CurrentStepName { get; init; }
}

public sealed class PipelineRunner
{
    private readonly AnalysisStepRegistry _registry;

    public PipelineRunner(AnalysisStepRegistry registry) => _registry = registry;

    public async Task<AnalysisReportModel> RunAsync(
        AnalysisPlan plan,
        AnalysisInputContext input,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var enabledSteps = plan.Steps.Where(s => s.Enabled).ToList();
        if (enabledSteps.Count == 0)
            throw new InvalidOperationException("请至少启用一个分析步骤。");

        var executionInput = new AnalysisInputContext
        {
            FilePath = input.FilePath,
            FileName = input.FileName,
            GroupName = input.GroupName,
            ChannelName = input.ChannelName,
            Samples = input.Samples,
            SampleRateHz = input.SampleRateHz,
            Sources = input.Sources,
            Data = input.Data,
            GlobalTimeRange = plan.GlobalTimeRange
        };

        var maxDurationSec = input.Sources.Count == 0
            ? 0
            : input.Sources.Max(s => s.Samples.Length / s.SampleRateHz);
        var globalRangeSummary = plan.GlobalTimeRange == null || plan.GlobalTimeRange.IsFullSegment(maxDurationSec)
            ? null
            : plan.GlobalTimeRange.FormatSummary(maxDurationSec);

        var cards = new List<ChartCardModel>();
        var completed = 0;

        foreach (var stepPlan in enabledSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var step = _registry.Resolve(stepPlan.StepType);

            progress?.Report(new PipelineProgress
            {
                CompletedSteps = completed,
                TotalSteps = enabledSteps.Count,
                CurrentStepName = step.Definition.DisplayName
            });

            var stepCards = await step.ExecuteAsync(executionInput, stepPlan.Parameters, cancellationToken)
                .ConfigureAwait(false);

            for (var i = 0; i < stepCards.Count; i++)
            {
                var card = stepCards[i];
                var baseId = string.IsNullOrWhiteSpace(stepPlan.Id) ? card.Id : stepPlan.Id;
                var id = stepCards.Count == 1 ? baseId : $"{baseId}-{i + 1}";
                cards.Add(card with { Id = id });
            }

            completed++;
        }

        return new AnalysisReportModel
        {
            Meta = new ReportMeta
            {
                Title = plan.Name,
                FileName = input.FileName,
                ChannelName = input.ChannelName,
                GroupName = input.GroupName,
                SampleRateHz = input.SampleRateHz,
                SampleCount = input.Sources.Max(s => s.Samples.Length),
                GeneratedAt = DateTime.Now,
                PlanName = plan.Name,
                AnalyzedStartSec = plan.GlobalTimeRange?.StartSec,
                AnalyzedEndSec = plan.GlobalTimeRange?.EndSec,
                TimeRangeSummary = globalRangeSummary
            },
            Cards = cards,
            Sources = input.Sources
                .Select(s => new ReportSourceInfo
                {
                    FilePath = s.FilePath,
                    FileName = s.FileName,
                    Color = s.Color
                })
                .ToList()
        };
    }
}
