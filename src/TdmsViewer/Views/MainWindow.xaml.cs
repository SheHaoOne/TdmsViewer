using System.IO;
using System.Windows;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            && GetTdmsFromDrop(e.Data) != null
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        var path = GetTdmsFromDrop(e.Data);
        if (path != null && DataContext is MainViewModel vm)
            vm.OpenFile(path);
    }

    private static string? GetTdmsFromDrop(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
            return null;

        if (data.GetData(DataFormats.FileDrop) is not string[] files)
            return null;

        return files.FirstOrDefault(f =>
            File.Exists(f) &&
            string.Equals(Path.GetExtension(f), ".tdms", StringComparison.OrdinalIgnoreCase));
    }
}
