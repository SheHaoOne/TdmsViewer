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

    public async Task<AnalysisReport> RunAsync(
        AnalysisPlan plan,
        AnalysisInputContext input,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var enabledSteps = plan.Steps.Where(s => s.Enabled).ToList();
        if (enabledSteps.Count == 0)
            throw new InvalidOperationException("请至少启用一个分析步骤。");

        var blocks = new List<ReportBlock>();
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

            var stepBlocks = await step.ExecuteAsync(input, cancellationToken).ConfigureAwait(false);
            foreach (var block in stepBlocks)
            {
                if (!string.IsNullOrWhiteSpace(stepPlan.Id))
                {
                    blocks.Add(new ReportBlock
                    {
                        BlockId = stepPlan.Id,
                        WidgetType = block.WidgetType,
                        Title = block.Title,
                        Payload = block.Payload,
                        Status = block.Status
                    });
                }
                else
                {
                    blocks.Add(block);
                }
            }

            completed++;
        }

        progress?.Report(new PipelineProgress
        {
            CompletedSteps = completed,
            TotalSteps = enabledSteps.Count,
            CurrentStepName = "完成"
        });

        var blockIds = blocks.Select(b => b.BlockId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var layout = DefaultDashboardLayout.ForTemplate(plan.DashboardTemplateId, blockIds);

        return new AnalysisReport
        {
            Meta = new ReportMeta
            {
                Title = plan.Name,
                FileName = input.FileName,
                ChannelName = input.ChannelName,
                GroupName = input.GroupName,
                SampleRateHz = input.SampleRateHz,
                SampleCount = input.Samples.Length,
                GeneratedAt = DateTime.Now,
                PlanName = plan.Name
            },
            Blocks = blocks,
            Layout = layout
        };
    }
}
