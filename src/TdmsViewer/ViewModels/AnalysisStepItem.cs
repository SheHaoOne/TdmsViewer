using CommunityToolkit.Mvvm.ComponentModel;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisStepItem : ObservableObject
{
    public required string StepType { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }

    [ObservableProperty]
    private bool _isEnabled = true;
}
