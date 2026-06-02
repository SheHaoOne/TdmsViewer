using CommunityToolkit.Mvvm.ComponentModel;

namespace TdmsViewer.ViewModels;

public partial class TdmsFileListItem : ObservableObject
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public int ChannelCount { get; init; }

    /// <summary>勾选后在波形图中叠加显示。</summary>
    [ObservableProperty]
    private bool _isVisibleOnPlot = true;

    /// <summary>与波形曲线一致的线条颜色（十六进制）。</summary>
    [ObservableProperty]
    private string _plotColor = "#007AFF";
}
