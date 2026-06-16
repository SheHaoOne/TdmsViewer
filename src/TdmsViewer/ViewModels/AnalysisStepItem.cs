namespace TdmsViewer.ViewModels;

public sealed class AnalysisStepItem
{
    public required string StepType { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public bool IsEnabled { get; set; }
}
