using System.IO;
using System.Windows;
using TdmsViewer.ViewModels;
using TdmsViewer.Views;

namespace TdmsViewer;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _mainViewModel = new MainViewModel();
        var window = new MainWindow { DataContext = _mainViewModel };
        MainWindow = window;
        window.Show();

        var filePath = ResolveTdmsPathFromArgs(e.Args);
        if (!string.IsNullOrEmpty(filePath))
            _mainViewModel.OpenFile(filePath);
    }

    /// <summary>
    /// 从命令行参数解析 .tdms 路径（支持双击关联与拖放启动）。
    /// </summary>
    internal static string? ResolveTdmsPathFromArgs(string[] args)
    {
        if (args.Length == 0)
            return null;

        foreach (var arg in args)
        {
            var path = arg.Trim('"');
            if (File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".tdms", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(path);
        }

        return null;
    }
}
