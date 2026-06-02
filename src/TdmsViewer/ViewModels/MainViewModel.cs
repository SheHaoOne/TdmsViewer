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
    private readonly AudioService _audioService = new();

    private string? _filePath;
    private double[]? _currentChannelData;

    [ObservableProperty]
    private string _windowTitle = "TdmsViewer";

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "请打开或导入 TDMS 文件";

    [ObservableProperty]
    private TdmsChannelInfo? _selectedChannel;

    [ObservableProperty]
    private int _currentPage;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalSamples;

    public string PageInfo => TotalPages <= 0 ? "—" : $"第 {CurrentPage + 1} / {TotalPages} 页";

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageInfo));

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isFileAssociationRegistered;

    public ObservableCollection<TdmsChannelInfo> Channels { get; } = new();
    public ObservableCollection<ChannelPropertyCard> PropertyCards { get; } = new();
    public ObservableCollection<DataPageRow> PageRows { get; } = new();
    public ObservableCollection<WaveformPoint> WaveformPoints { get; } = new();

    public MainViewModel()
    {
        IsFileAssociationRegistered = FileAssociationService.IsRegistered();
    }

    partial void OnSelectedChannelChanged(TdmsChannelInfo? value)
    {
        if (value != null && _filePath != null)
            _ = LoadChannelAsync(value);
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "TDMS 文件 (*.tdms)|*.tdms|所有文件 (*.*)|*.*",
            Title = "打开 TDMS 文件"
        };

        if (dialog.ShowDialog() == true)
            OpenFile(dialog.FileName);
    }

    [RelayCommand]
    private void ImportFile() => OpenFile();

    public async void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            StatusMessage = "文件不存在";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "正在加载…";
            _filePath = path;
            CurrentFilePath = path;
            WindowTitle = $"TdmsViewer — {Path.GetFileName(path)}";

            var channels = await Task.Run(() => _tdmsService.LoadChannels(path));

            Channels.Clear();
            foreach (var ch in channels)
                Channels.Add(ch);

            HasFile = Channels.Count > 0;
            if (!HasFile)
            {
                StatusMessage = "文件中未找到通道";
                return;
            }

            SelectedChannel = Channels[0];
            StatusMessage = $"已加载 {Channels.Count} 个通道";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开文件：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "加载失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadChannelAsync(TdmsChannelInfo channel)
    {
        if (_filePath == null)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = $"正在读取通道 {channel.ChannelName}…";

            _currentChannelData = await Task.Run(() =>
                _tdmsService.ReadChannelData(_filePath, channel));

            TotalSamples = _currentChannelData.Length;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalSamples / (double)PageSize));
            CurrentPage = 0;

            RefreshPropertyCards(channel);
            RefreshPage();
            RefreshWaveform(channel);

            StatusMessage = $"通道 {channel.DisplayName} — {TotalSamples:N0} 个采样点";
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

    private void RefreshWaveform(TdmsChannelInfo channel)
    {
        WaveformPoints.Clear();
        if (_currentChannelData == null)
            return;

        foreach (var pt in _tdmsService.BuildWaveform(_currentChannelData, channel.SampleRateHz))
            WaveformPoints.Add(pt);
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
        if (_currentChannelData == null || SelectedChannel == null)
            return;

        try
        {
            _audioService.PlayFromChannelData(_currentChannelData, SelectedChannel.SampleRateHz);
            IsPlaying = true;
            StatusMessage = "正在播放通道音频…";
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
        if (_currentChannelData == null || SelectedChannel == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "WAV 音频 (*.wav)|*.wav",
            FileName = $"{SelectedChannel.ChannelName}.wav",
            Title = "导出音频"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _audioService.ExportWav(dialog.FileName, _currentChannelData, SelectedChannel.SampleRateHz);
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
            StatusMessage = "已注册 .tdms 文件关联，双击即可打开";
            MessageBox.Show(
                "已将 .tdms 文件关联到 TdmsViewer。\n双击 .tdms 文件即可直接打开查看。",
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
