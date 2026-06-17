namespace TdmsViewer.Analysis.Parameters;

public sealed class AnalysisParameterDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required AnalysisParameterKind Kind { get; init; }
    public required object DefaultValue { get; init; }
    public IReadOnlyList<AnalysisChoiceOption> Choices { get; init; } = Array.Empty<AnalysisChoiceOption>();
}
