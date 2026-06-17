using System.IO;
using System.Windows;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MainWaveform.TimeRangeSelected += OnWaveformTimeRangeSelected;
    }

    private void OnWaveformTimeRangeSelected(object? sender, Controls.WaveformTimeRangeSelectedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Workbench.SetGlobalTimeRangeFromWaveform(e.StartSec, e.EndSec);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && GetTdmsPathsFromDrop(e.Data).Count > 0
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        var paths = GetTdmsPathsFromDrop(e.Data);
        if (paths.Count > 0 && DataContext is MainViewModel vm)
            vm.ImportFilesFromPaths(paths, replaceSession: false);
    }

    private static IReadOnlyList<string> GetTdmsPathsFromDrop(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
            return Array.Empty<string>();

        if (data.GetData(DataFormats.FileDrop) is not string[] files)
            return Array.Empty<string>();

        return files
            .Where(f => File.Exists(f) &&
                        string.Equals(Path.GetExtension(f), ".tdms", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
