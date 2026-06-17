using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;
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
    private readonly List<WaveformSeries> _cachedOverlaySeries = new();
    private double[]? _currentChannelData;
    private ChannelSourceRef? _activeSource;
    private bool _suppressSelectAllSync;
    private bool _suppressActiveFileChanged;
    private bool _suppressSelectedGroupChanged;
    private int _channelLoadGeneration;

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
    private TdmsFileListItem? _activeFile;

    [ObservableProperty]
    private TdmsGroupInfo? _selectedGroup;

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
    private AppViewMode _currentViewMode = AppViewMode.Viewer;

    /// <summary>全选复选框：勾选则全部参与波形叠加，取消则全部不叠加。</summary>
    [ObservableProperty]
    private bool _isAllFilesOverlayChecked = true;

    public AnalysisWorkbenchViewModel Workbench { get; }
    public AnalysisReportViewModel Report { get; }

    public string PageInfo => TotalPages <= 0 ? "—" : $"第 {CurrentPage + 1} / {TotalPages} 页";

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    partial void OnIsAllFilesOverlayCheckedChanged(bool value)
    {
        if (_suppressSelectAllSync)
            return;

        foreach (var file in LoadedFiles)
            file.IsVisibleOnPlot = value;

        ApplyOverlayVisibility();
    }

    public ObservableCollection<TdmsFileListItem> LoadedFiles { get; } = new();
    public ObservableCollection<TdmsGroupInfo> Groups { get; } = new();
    public ObservableCollection<MergedChannelInfo> MergedChannels { get; } = new();
    public ObservableCollection<ChannelPropertyCard> GroupPropertyCards { get; } = new();
    public ObservableCollection<ChannelPropertyCard> PropertyCards { get; } = new();
    public ObservableCollection<DataPageRow> PageRows { get; } = new();
    public ObservableCollection<WaveformSeries> WaveformSeries { get; } = new();

    public MainViewModel()
    {
        IsFileAssociationRegistered = FileAssociationService.IsRegistered();
        _audioService.PlaybackStopped += (_, _) => IsPlaying = false;

        Report = new AnalysisReportViewModel();
        Workbench = new AnalysisWorkbenchViewModel(
            LoadAnalysisInputAsync,
            CanAnalyze,
            OnAnalysisReportReady);

        NvhLicenseService.TryLoad();
        RefreshAnalysisContext();
    }

    partial void OnSelectedChannelChanged(MergedChannelInfo? value)
    {
        if (value != null)
            _ = LoadMergedChannelAsync(value);

        RefreshAnalysisContext();
    }

    partial void OnCurrentViewModeChanged(AppViewMode value)
    {
        if (value == AppViewMode.Analysis)
            RefreshAnalysisContext();
    }

    partial void OnActiveFileChanged(TdmsFileListItem? value)
    {
        if (!_suppressActiveFileChanged)
            RefreshGroupsForActiveFile(value);

        if (_suppressActiveFileChanged || value == null)
            return;

        if (SelectedChannel != null)
            LoadActiveFileData(value);

        if (!LoadedFiles.Any(f => f.IsVisibleOnPlot))
            ApplyOverlayVisibility();
    }

    partial void OnSelectedGroupChanged(TdmsGroupInfo? value)
    {
        if (_suppressSelectedGroupChanged)
            return;

        RefreshGroupPropertyCards(value);
        RebuildMergedChannels(selectFirst: true);
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

        UnsubscribeFileItem(item);
        _loadedFiles.RemoveAll(f => string.Equals(f.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase));

        if (_loadedFiles.Count == 0)
        {
            RefreshFileListUi();
            MergedChannels.Clear();
            ClearDetailPanels();
            SelectedChannel = null;
            SelectedGroup = null;
            HasFile = false;
            SessionSummary = null;
            WindowTitle = "TdmsViewer";
            StatusMessage = "请批量导入 TDMS 文件进行对比";
            return;
        }

        RefreshFileListUi();
        RefreshGroupsForActiveFile(ActiveFile);
        WindowTitle = $"TdmsViewer — {_loadedFiles.Count} 个文件";
        StatusMessage = $"已加载 {_loadedFiles.Count} 个文件，{MergedChannels.Count} 个通道";
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
            {
                foreach (var item in LoadedFiles.ToList())
                    UnsubscribeFileItem(item);
                _loadedFiles.Clear();
            }

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
            SyncSelectAllCheckbox();
            if (SelectedGroup == null && ActiveFile != null)
                RefreshGroupsForActiveFile(ActiveFile);
            else
                RebuildMergedChannels(selectFirst: SelectedChannel == null);
            WindowTitle = $"TdmsViewer — {_loadedFiles.Count} 个文件";
            StatusMessage = $"已加载 {_loadedFiles.Count} 个文件，{MergedChannels.Count} 个通道";
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
                entries.Add(_tdmsService.LoadFile(path));
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
        var previousKey = SelectedChannel?.ChannelKey;

        MergedChannels.Clear();

        if (SelectedGroup == null)
        {
            HasFile = _loadedFiles.Count > 0;
            SessionSummary = HasFile ? $"{_loadedFiles.Count} 个文件" : null;
            SelectedChannel = null;
            if (!HasFile)
                ClearDetailPanels();
            return;
        }

        var merged = _mergeService.MergeChannels(_loadedFiles, SelectedGroup.GroupName);
        foreach (var ch in merged)
            MergedChannels.Add(ch);

        HasFile = _loadedFiles.Count > 0;
        SessionSummary = HasFile
            ? $"{_loadedFiles.Count} 个文件 · {SelectedGroup.GroupName} · {MergedChannels.Count} 个通道"
            : null;

        if (MergedChannels.Count == 0)
        {
            SelectedChannel = null;
            ClearChannelDetailPanels();
            return;
        }

        SelectedChannel = selectFirst
            ? MergedChannels[0]
            : MergedChannels.FirstOrDefault(c => c.ChannelKey == previousKey) ?? MergedChannels[0];
    }

    private void RefreshFileListUi()
    {
        var overlayState = LoadedFiles.ToDictionary(
            f => f.FilePath,
            f => f.IsVisibleOnPlot,
            StringComparer.OrdinalIgnoreCase);

        var previousDataPath = ActiveFile?.FilePath;

        foreach (var item in LoadedFiles.ToList())
            UnsubscribeFileItem(item);

        LoadedFiles.Clear();

        foreach (var f in _loadedFiles.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase))
        {
            var item = new TdmsFileListItem
            {
                FilePath = f.FilePath,
                FileName = f.FileName,
                ChannelCount = f.Channels.Count,
                IsVisibleOnPlot = overlayState.GetValueOrDefault(f.FilePath, true)
            };
            SubscribeFileItem(item);
            LoadedFiles.Add(item);
        }

        UpdateFilePlotColors();

        var nextActiveFile = LoadedFiles.FirstOrDefault(f =>
                                 string.Equals(f.FilePath, previousDataPath, StringComparison.OrdinalIgnoreCase))
                             ?? LoadedFiles.FirstOrDefault();
        var activeFilePathChanged = !string.Equals(
            previousDataPath,
            nextActiveFile?.FilePath,
            StringComparison.OrdinalIgnoreCase);

        _suppressActiveFileChanged = true;
        ActiveFile = nextActiveFile;
        _suppressActiveFileChanged = false;

        if (activeFilePathChanged && ActiveFile != null)
            RefreshGroupsForActiveFile(ActiveFile);
    }

    private void UpdateFilePlotColors()
    {
        for (var i = 0; i < LoadedFiles.Count; i++)
            LoadedFiles[i].PlotColor = PlotColorPalette.GetColorForFileIndex(i);
    }

    private string GetPlotColorForFile(string filePath)
    {
        var index = LoadedFiles.ToList().FindIndex(f =>
            string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        return PlotColorPalette.GetColorForFileIndex(index < 0 ? 0 : index);
    }

    private void SubscribeFileItem(TdmsFileListItem item)
    {
        item.PropertyChanged += OnFileItemPropertyChanged;
    }

    private void UnsubscribeFileItem(TdmsFileListItem item)
    {
        item.PropertyChanged -= OnFileItemPropertyChanged;
    }

    private void OnFileItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not TdmsFileListItem item)
            return;

        if (e.PropertyName == nameof(TdmsFileListItem.IsVisibleOnPlot))
        {
            SyncSelectAllCheckbox();
            ApplyOverlayVisibility();
        }
    }

    private void SyncSelectAllCheckbox()
    {
        _suppressSelectAllSync = true;
        IsAllFilesOverlayChecked = LoadedFiles.Count > 0 && LoadedFiles.All(f => f.IsVisibleOnPlot);
        _suppressSelectAllSync = false;
    }

    private void LoadActiveFileData(TdmsFileListItem file)
    {
        if (SelectedChannel == null)
            return;

        var source = ResolveSourceForFile(file);
        if (source == null)
        {
            StatusMessage = $"{file.FileName} 中不含当前通道";
            PropertyCards.Clear();
            PageRows.Clear();
            _currentChannelData = null;
            _activeSource = null;
            return;
        }

        _ = LoadSourceDataAsync(source);
    }

    private ChannelSourceRef? ResolveSourceForFile(TdmsFileListItem file)
    {
        if (SelectedChannel == null)
            return null;

        var matches = SelectedChannel.Sources
            .Where(s => string.Equals(s.FilePath, file.FilePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => matches.FirstOrDefault(m => IsSameSource(m, _activeSource)) ?? matches[0]
        };
    }

    private static bool IsSameSource(ChannelSourceRef a, ChannelSourceRef? b)
    {
        if (b == null)
            return false;

        return string.Equals(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase)
               && string.Equals(a.Channel.GroupName, b.Channel.GroupName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(a.Channel.ChannelName, b.Channel.ChannelName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadMergedChannelAsync(MergedChannelInfo merged)
    {
        var generation = ++_channelLoadGeneration;

        try
        {
            IsBusy = true;
            StatusMessage = $"正在加载通道 {merged.DisplayName}…";

            _cachedOverlaySeries.Clear();
            var seriesList = await Task.Run(() => BuildOverlaySeries(merged));

            if (generation != _channelLoadGeneration)
                return;

            _cachedOverlaySeries.Clear();
            _cachedOverlaySeries.AddRange(seriesList);
            ApplyOverlayVisibility();

            var dataFile = ActiveFile != null &&
                           merged.Sources.Any(s => string.Equals(s.FilePath, ActiveFile.FilePath, StringComparison.OrdinalIgnoreCase))
                ? ActiveFile
                : LoadedFiles.FirstOrDefault(f =>
                      merged.Sources.Any(s => string.Equals(s.FilePath, f.FilePath, StringComparison.OrdinalIgnoreCase)));

            if (dataFile != null)
            {
                var source = ResolveSourceForFile(dataFile);
                _suppressActiveFileChanged = true;
                ActiveFile = dataFile;
                _suppressActiveFileChanged = false;
                ApplyOverlayVisibility();

                if (source != null)
                    await LoadSourceDataAsync(source, generation);
            }

            if (generation != _channelLoadGeneration)
                return;

            ApplyOverlayVisibility();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载波形失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (generation == _channelLoadGeneration)
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
                SeriesKey = $"{src.FilePath}|{src.Channel.GroupName}|{src.Channel.ChannelName}",
                FilePath = src.FilePath,
                Label = src.FileName,
                Color = GetPlotColorForFile(src.FilePath),
                Points = points
            });
        }

        return result;
    }

    private void ApplyOverlayVisibility()
    {
        var checkedFiles = LoadedFiles.Where(f => f.IsVisibleOnPlot).ToList();
        HashSet<string> visiblePaths;

        if (checkedFiles.Count > 0)
        {
            visiblePaths = checkedFiles
                .Select(f => f.FilePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (ActiveFile != null)
        {
            visiblePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ActiveFile.FilePath };
        }
        else
        {
            visiblePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        WaveformSeries.Clear();
        foreach (var series in _cachedOverlaySeries.Where(s => visiblePaths.Contains(s.FilePath)))
            WaveformSeries.Add(series);

        if (SelectedChannel == null)
            return;

        var visibleCount = WaveformSeries.Count;
        StatusMessage = checkedFiles.Count > 0
            ? $"通道 {SelectedChannel.DisplayName} — 叠加 {visibleCount} 条波形"
            : ActiveFile != null
                ? $"通道 {SelectedChannel.DisplayName} — {ActiveFile.FileName}"
                : $"通道 {SelectedChannel.DisplayName}";
    }

    private Task LoadSourceDataAsync(ChannelSourceRef source) =>
        LoadSourceDataAsync(source, _channelLoadGeneration);

    private async Task LoadSourceDataAsync(ChannelSourceRef source, int generation)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"正在读取 {source.FileName} …";

            var data = await Task.Run(() =>
                _tdmsService.ReadChannelData(source.FilePath, source.Channel));

            if (generation != _channelLoadGeneration)
                return;

            _activeSource = source;
            _currentChannelData = data;
            TotalSamples = _currentChannelData.Length;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalSamples / (double)PageSize));
            CurrentPage = 0;

            RefreshPropertyCards(source.Channel);
            RefreshPage();

            StatusMessage = $"数据：{source.FileName} — {TotalSamples:N0} 个采样点";
            RefreshAnalysisContext();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取通道失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (generation == _channelLoadGeneration)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenAnalysis()
    {
        if (!HasFile || SelectedChannel == null)
        {
            MessageBox.Show("请先导入文件并选择通道。", "数据分析", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        RefreshAnalysisContext();
        CurrentViewMode = AppViewMode.Analysis;
        StatusMessage = "数据分析模式";
    }

    [RelayCommand]
    private void ShowViewer()
    {
        CurrentViewMode = AppViewMode.Viewer;
        StatusMessage = SelectedChannel == null
            ? "请批量导入 TDMS 文件进行对比"
            : $"通道 {SelectedChannel.DisplayName}";
    }

    [RelayCommand]
    private void ShowReport()
    {
        if (Report.Cards.Count == 0)
        {
            MessageBox.Show("请先运行分析。", "分析报表", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CurrentViewMode = AppViewMode.Report;
        StatusMessage = "分析报表";
    }

    private void OnAnalysisReportReady(AnalysisReportModel report)
    {
        Report.SetReport(report);
        CurrentViewMode = AppViewMode.Report;
        StatusMessage = $"分析完成：{report.Cards.Count} 张图表";
    }

    private void RefreshAnalysisContext()
    {
        Workbench.RefreshChannelSummary(DescribeAnalysisTarget());
        Workbench.NotifyCanAnalyzeChanged();
    }

    private bool CanAnalyze() => DescribeAnalysisTarget() != null;

    private AnalysisTargetDescription? DescribeAnalysisTarget()
    {
        var sources = GetAllAnalysisSources();
        if (sources.Count == 0)
            return null;

        var maxSampleCount = 0;
        double? sampleRate = null;
        foreach (var source in sources)
        {
            var sampleCount = GetKnownSampleCount(source);
            if (sampleCount <= 0)
                return null;

            maxSampleCount = Math.Max(maxSampleCount, sampleCount);
            sampleRate ??= source.Channel.SampleRateHz ?? 44100;
        }

        var channelName = SelectedChannel?.ChannelName ?? sources[0].Channel.ChannelName;
        return new AnalysisTargetDescription
        {
            FileName = FormatAnalysisTargetLabel(sources, channelName),
            GroupName = sources[0].Channel.GroupName,
            ChannelName = channelName,
            SampleRateHz = sampleRate ?? 44100,
            SampleCount = maxSampleCount,
            SourceCount = sources.Count
        };
    }

    private async Task<AnalysisInputContext?> LoadAnalysisInputAsync() =>
        await Task.Run(BuildAnalysisInput);

    private IReadOnlyList<ChannelSourceRef> GetAllAnalysisSources()
    {
        if (SelectedChannel == null)
            return Array.Empty<ChannelSourceRef>();

        return SelectedChannel.Sources.ToList();
    }

    private int GetKnownSampleCount(ChannelSourceRef source)
    {
        if (_currentChannelData != null && _activeSource != null && IsSameSource(_activeSource, source))
            return _currentChannelData.Length;

        return source.Channel.SampleCount > int.MaxValue
            ? int.MaxValue
            : (int)source.Channel.SampleCount;
    }

    private AnalysisInputContext? BuildAnalysisInput()
    {
        var sources = GetAllAnalysisSources();
        if (sources.Count == 0)
            return null;

        var samples = new List<AnalysisSourceSample>(sources.Count);
        foreach (var source in sources)
        {
            var data = ReadSourceSamples(source);
            if (data == null || data.Length == 0)
                continue;

            samples.Add(new AnalysisSourceSample
            {
                FilePath = source.FilePath,
                FileName = source.FileName,
                GroupName = source.Channel.GroupName,
                ChannelName = source.Channel.ChannelName,
                Samples = data,
                SampleRateHz = source.Channel.SampleRateHz ?? 44100,
                Label = source.FileName,
                Color = GetPlotColorForFile(source.FilePath)
            });
        }

        if (samples.Count == 0)
            return null;

        var primary = samples[0];
        return new AnalysisInputContext
        {
            FilePath = primary.FilePath,
            FileName = FormatAnalysisTargetLabel(sources, primary.ChannelName),
            GroupName = primary.GroupName,
            ChannelName = primary.ChannelName,
            Samples = primary.Samples,
            SampleRateHz = primary.SampleRateHz,
            Sources = samples,
            Data = new AnalysisDataAccessor(_tdmsService, _loadedFiles)
        };
    }

    private double[]? ReadSourceSamples(ChannelSourceRef source)
    {
        if (_currentChannelData != null && _activeSource != null && IsSameSource(_activeSource, source))
            return _currentChannelData.ToArray();

        try
        {
            return _tdmsService.ReadChannelData(source.FilePath, source.Channel);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatAnalysisTargetLabel(IReadOnlyList<ChannelSourceRef> sources, string channelName) =>
        sources.Count <= 1
            ? sources[0].FileName
            : $"{channelName}（{sources.Count} 个文件叠加）";

    private void RefreshGroupsForActiveFile(TdmsFileListItem? file)
    {
        _suppressSelectedGroupChanged = true;
        try
        {
            Groups.Clear();
            GroupPropertyCards.Clear();

            if (file == null)
            {
                SelectedGroup = null;
                return;
            }

            var entry = _loadedFiles.FirstOrDefault(f =>
                string.Equals(f.FilePath, file.FilePath, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                SelectedGroup = null;
                return;
            }

            foreach (var group in entry.Groups.OrderBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase))
                Groups.Add(group);

            SelectedGroup = Groups.FirstOrDefault();
        }
        finally
        {
            _suppressSelectedGroupChanged = false;
        }

        RefreshGroupPropertyCards(SelectedGroup);
        RebuildMergedChannels(selectFirst: true);
    }

    private void RefreshGroupPropertyCards(TdmsGroupInfo? group)
    {
        GroupPropertyCards.Clear();
        if (group == null)
            return;

        foreach (var card in _tdmsService.BuildGroupPropertyCards(group))
            GroupPropertyCards.Add(card);
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

    private void ClearChannelDetailPanels()
    {
        WaveformSeries.Clear();
        _cachedOverlaySeries.Clear();
        PropertyCards.Clear();
        PageRows.Clear();
        _currentChannelData = null;
        _activeSource = null;
    }

    private void ClearDetailPanels()
    {
        ClearChannelDetailPanels();
        Groups.Clear();
        GroupPropertyCards.Clear();
    }

    private void ResetSession()
    {
        foreach (var item in LoadedFiles.ToList())
            UnsubscribeFileItem(item);

        _loadedFiles.Clear();
        LoadedFiles.Clear();
        MergedChannels.Clear();
        ClearDetailPanels();
        SelectedChannel = null;
        ActiveFile = null;
        SelectedGroup = null;
        HasFile = false;
        SessionSummary = null;
        IsAllFilesOverlayChecked = true;
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
        if (SelectedChannel == null || ActiveFile == null)
        {
            MessageBox.Show("请先选择通道，并单击左侧文件名。", "播放音频", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (!TryEnsureChannelDataForPlayback(out var data, out var source))
                return;

            _audioService.PlayFromChannelData(data, source.Channel.SampleRateHz);
            IsPlaying = true;
            StatusMessage = $"正在播放 {source.FileName} …";
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            MessageBox.Show($"播放失败：{ex.Message}", "音频", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool TryEnsureChannelDataForPlayback(out double[] data, out ChannelSourceRef source)
    {
        data = Array.Empty<double>();
        source = null!;

        if (ActiveFile == null || SelectedChannel == null)
            return false;

        var resolved = ResolveSourceForFile(ActiveFile);
        if (resolved == null)
        {
            MessageBox.Show($"{ActiveFile.FileName} 中不含当前通道。", "播放音频", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        source = resolved;

        if (_currentChannelData != null && _activeSource != null && IsSameSource(_activeSource, source))
        {
            if (_currentChannelData.Length == 0)
            {
                MessageBox.Show("当前通道没有可播放的数据。", "播放音频", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            data = _currentChannelData;
            return true;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"正在读取 {source.FileName} 用于播放…";
            _activeSource = source;
            _currentChannelData = _tdmsService.ReadChannelData(source.FilePath, source.Channel);
            RefreshPropertyCards(source.Channel);
            TotalSamples = _currentChannelData.Length;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalSamples / (double)PageSize));
            CurrentPage = 0;
            RefreshPage();
            data = _currentChannelData;

            if (data.Length == 0)
            {
                MessageBox.Show("当前通道没有可播放的数据。", "播放音频", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取通道数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            IsBusy = false;
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
        if (SelectedChannel == null || ActiveFile == null)
        {
            MessageBox.Show("请先选择通道并单击文件名。", "导出音频", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryEnsureChannelDataForPlayback(out _, out var source))
            return;

        if (_currentChannelData == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "WAV 音频 (*.wav)|*.wav",
            FileName = $"{source.Channel.ChannelName}_{source.FileName}.wav",
            Title = "导出音频"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _audioService.ExportWav(dialog.FileName, _currentChannelData, source.Channel.SampleRateHz);
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
