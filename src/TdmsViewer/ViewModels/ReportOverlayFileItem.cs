using CommunityToolkit.Mvvm.ComponentModel;

namespace TdmsViewer.ViewModels;

public sealed partial class ReportOverlayFileItem : ObservableObject
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required string PlotColor { get; init; }

    [ObservableProperty]
    private bool _isVisible = true;
}
