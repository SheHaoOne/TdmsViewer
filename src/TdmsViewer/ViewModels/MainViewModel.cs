using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Models;
using TdmsViewer.Services;

namespace TdmsViewer.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private const int PageSize = 100;

    private readonly TdmsFileService _tdmsService = new();
    private readonly ChannelMergeService _mergeService = new();
    private readonly AudioService _audioService = new();

    private readonly List<TdmsFileEntry> _loadedFiles = new();
    private double[]? _currentChannelData;

    [ObservableProperty]
    private string _windowTitle = "TdmsViewer";

    [ObservableProperty]
    private string? _sessionSummary;

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "请批量导入 TDMS 文件进行对比";

    [ObservableProperty]
    private MergedChannelInfo? _selectedChannel;

    [ObservableProperty]
    private ChannelSourceRef? _selectedSource;

    [ObservableProperty]
    private int _currentPage;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalSamples;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isFileAssociationRegistered;

    [ObservableProperty]
    private bool _hasMultipleSources;

    public string PageInfo => TotalPages <= 0 ? "—" : $"第 {CurrentPage + 1} / {TotalPages} 页";

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    public ObservableCollection<TdmsFileListItem> LoadedFiles { get; } = new();
    public ObservableCollection<MergedChannelInfo> MergedChannels { get; } = new();
    public ObservableCollection<ChannelSourceRef> AvailableSources { get; } = new();
    public ObservableCollection<ChannelPropertyCard> PropertyCards { get; } = new();
    public ObservableCollection<DataPageRow> PageRows { get; } = new();
    public ObservableCollection<WaveformSeries> WaveformSeries { get; } = new();

    public MainViewModel()
    {
        IsFileAssociationRegistered = FileAssociationService.IsRegistered();
    }

    partial void OnSelectedChannelChanged(MergedChannelInfo? value)
    {
        if (value != null)
            _ = LoadMergedChannelAsync(value);
    }

    partial void OnSelectedSourceChanged(ChannelSourceRef? value)
    {
        if (value != null && SelectedChannel != null)
            _ = LoadSourceDataAsync(value);
    }

    [RelayCommand]
    private void BatchImport()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "TDMS 文件 (*.tdms)|*.tdms|所有文件 (*.*)|*.*",
            Title = "批量导入 TDMS 文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            _ = ImportFilesAsync(dialog.FileNames, replaceSession: true);
    }

    [RelayCommand]
    private void ClearFiles() => ResetSession();

    [RelayCommand]
    private void RemoveFile(TdmsFileListItem? item)
    {
        if (item == null)
            return;

        _loadedFiles.RemoveAll(f => string.Equals(f.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase));
        RefreshFileListUi();
        RebuildMergedChannels(selectFirst: true);
    }

    public void ImportFilesFromPaths(IEnumerable<string> paths, bool replaceSession = false) =>
        _ = ImportFilesAsync(paths, replaceSession);

    private async Task ImportFilesAsync(IEnumerable<string> paths, bool replaceSession)
    {
        var pathList = paths
            .Select(p => p.Trim('"'))
            .Where(p => File.Exists(p) &&
                        string.Equals(Path.GetExtension(p), ".tdms", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pathList.Count == 0)
        {
            StatusMessage = "未选择有效的 TDMS 文件";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"正在加载 {pathList.Count} 个文件…";

            var newEntries = await Task.Run(() => LoadFileEntries(pathList));

            if (replaceSession)
                _loadedFiles.Clear();

            foreach (var entry in newEntries)
            {
                if (_loadedFiles.Any(f => string.Equals(f.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase)))
                    continue;
                _loadedFiles.Add(entry);
            }

            if (_loadedFiles.Count == 0)
            {
                StatusMessage = "未能加载任何通道";
                HasFile = false;
                return;
            }

            RefreshFileListUi();
            RebuildMergedChannels(selectFirst: SelectedChannel == null);
            WindowTitle = $"TdmsViewer — {_loadedFiles.Count} 个文件";
            StatusMessage = $"已加载 {_loadedFiles.Count} 个文件，{MergedChannels.Count} 个通道（可叠加对比）";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"批量导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "加载失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<TdmsFileEntry> LoadFileEntries(IReadOnlyList<string> paths)
    {
        var entries = new List<TdmsFileEntry>(paths.Count);
        var errors = new List<string>();

        foreach (var path in paths)
        {
            try
            {
                var channels = _tdmsService.LoadChannels(path);
                entries.Add(new TdmsFileEntry
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    Channels = channels
                });
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        if (errors.Count > 0 && entries.Count > 0)
            MessageBox.Show(
                "部分文件加载失败：\n" + string.Join("\n", errors),
                "警告",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        else if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        return entries;
    }

    private void RebuildMergedChannels(bool selectFirst)
    {
        var merged = _mergeService.MergeChannels(_loadedFiles);
        var previousKey = SelectedChannel?.ChannelKey;

        MergedChannels.Clear();
        foreach (var ch in merged)
            MergedChannels.Add(ch);

        HasFile = MergedChannels.Count > 0;
        SessionSummary = HasFile
            ? $"{_loadedFiles.Count} 个文件 · {MergedChannels.Count} 个通道"
            : null;

        if (!HasFile)
        {
            SelectedChannel = null;
            ClearDetailPanels();
            return;
        }

        SelectedChannel = selectFirst
            ? MergedChannels[0]
            : MergedChannels.FirstOrDefault(c => c.ChannelKey == previousKey) ?? MergedChannels[0];
    }

    private void RefreshFileListUi()
    {
        LoadedFiles.Clear();
        foreach (var f in _loadedFiles.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase))
        {
            LoadedFiles.Add(new TdmsFileListItem
            {
                FilePath = f.FilePath,
                FileName = f.FileName,
                ChannelCount = f.Channels.Count
            });
        }
    }

    private async Task LoadMergedChannelAsync(MergedChannelInfo merged)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"正在叠加通道 {merged.DisplayName}（{merged.SourceCount} 个文件）…";

            AvailableSources.Clear();
            foreach (var src in merged.Sources)
                AvailableSources.Add(src);

            HasMultipleSources = merged.SourceCount > 1;

            var seriesList = await Task.Run(() => BuildOverlaySeries(merged));
            WaveformSeries.Clear();
            foreach (var s in seriesList)
                WaveformSeries.Add(s);

            var previousPath = SelectedSource?.FilePath;
            SelectedSource = merged.Sources.FirstOrDefault(s =>
                                 string.Equals(s.FilePath, previousPath, StringComparison.OrdinalIgnoreCase))
                             ?? merged.Sources[0];

            StatusMessage = merged.SourceCount > 1
                ? $"已叠加 {merged.SourceCount} 条波形 — {merged.DisplayName}"
                : $"通道 {merged.DisplayName} — 单文件";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载波形失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<WaveformSeries> BuildOverlaySeries(MergedChannelInfo merged)
    {
        var result = new List<WaveformSeries>(merged.Sources.Count);
        for (var i = 0; i < merged.Sources.Count; i++)
        {
            var src = merged.Sources[i];
            var data = _tdmsService.ReadChannelData(src.FilePath, src.Channel);
            var points = _tdmsService.BuildWaveform(data, src.Channel.SampleRateHz);
            result.Add(new WaveformSeries
            {
                Label = src.FileName,
                Color = PlotColorPalette.GetColor(i),
                Points = points
            });
        }

        return result;
    }

    private async Task LoadSourceDataAsync(ChannelSourceRef source)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"正在读取 {source.FileName} …";

            _currentChannelData = await Task.Run(() =>
                _tdmsService.ReadChannelData(source.FilePath, source.Channel));

            TotalSamples = _currentChannelData.Length;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalSamples / (double)PageSize));
            CurrentPage = 0;

            RefreshPropertyCards(source.Channel);
            RefreshPage();

            StatusMessage = $"数据：{source.FileName} — {TotalSamples:N0} 个采样点";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取通道失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshPropertyCards(TdmsChannelInfo channel)
    {
        PropertyCards.Clear();
        foreach (var card in _tdmsService.BuildPropertyCards(channel))
            PropertyCards.Add(card);
    }

    private void RefreshPage()
    {
        PageRows.Clear();
        if (_currentChannelData == null)
            return;

        foreach (var row in _tdmsService.GetPage(_currentChannelData, CurrentPage, PageSize, CultureInfo.CurrentCulture))
            PageRows.Add(row);
    }

    private void ClearDetailPanels()
    {
        WaveformSeries.Clear();
        PropertyCards.Clear();
        PageRows.Clear();
        AvailableSources.Clear();
        _currentChannelData = null;
        HasMultipleSources = false;
    }

    private void ResetSession()
    {
        _loadedFiles.Clear();
        LoadedFiles.Clear();
        MergedChannels.Clear();
        ClearDetailPanels();
        SelectedChannel = null;
        SelectedSource = null;
        HasFile = false;
        SessionSummary = null;
        WindowTitle = "TdmsViewer";
        StatusMessage = "请批量导入 TDMS 文件进行对比";
    }

    [RelayCommand]
    private void FirstPage()
    {
        if (CurrentPage == 0) return;
        CurrentPage = 0;
        RefreshPage();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage <= 0) return;
        CurrentPage--;
        RefreshPage();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages - 1) return;
        CurrentPage++;
        RefreshPage();
    }

    [RelayCommand]
    private void LastPage()
    {
        if (CurrentPage >= TotalPages - 1) return;
        CurrentPage = TotalPages - 1;
        RefreshPage();
    }

    [RelayCommand]
    private void PlayAudio()
    {
        if (_currentChannelData == null || SelectedSource == null)
            return;

        try
        {
            _audioService.PlayFromChannelData(_currentChannelData, SelectedSource.Channel.SampleRateHz);
            IsPlaying = true;
            StatusMessage = $"正在播放 {SelectedSource.FileName} …";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"播放失败：{ex.Message}", "音频", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void StopAudio()
    {
        _audioService.Stop();
        IsPlaying = false;
        StatusMessage = "已停止播放";
    }

    [RelayCommand]
    private void ExportAudio()
    {
        if (_currentChannelData == null || SelectedSource == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "WAV 音频 (*.wav)|*.wav",
            FileName = $"{SelectedSource.Channel.ChannelName}_{SelectedSource.FileName}.wav",
            Title = "导出音频"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _audioService.ExportWav(dialog.FileName, _currentChannelData, SelectedSource.Channel.SampleRateHz);
            StatusMessage = $"已导出 {dialog.FileName}";
            MessageBox.Show("音频导出成功。", "导出", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RegisterFileAssociation()
    {
        try
        {
            FileAssociationService.Register();
            IsFileAssociationRegistered = true;
            StatusMessage = "已注册 .tdms 文件关联";
            MessageBox.Show(
                "已将 .tdms 文件关联到 TdmsViewer。\n双击 .tdms 文件将加入批量查看会话。",
                "文件关联",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"注册失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void UnregisterFileAssociation()
    {
        try
        {
            FileAssociationService.Unregister();
            IsFileAssociationRegistered = false;
            StatusMessage = "已取消 .tdms 文件关联";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"取消注册失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Dispose() => _audioService.Dispose();
}

public sealed class TdmsFileListItem
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public int ChannelCount { get; init; }
}
