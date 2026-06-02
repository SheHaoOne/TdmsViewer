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

    /// <summary>单击文件名后用于属性/数据/音频的来源。</summary>
    [ObservableProperty]
    private bool _isSelectedForData;
}
