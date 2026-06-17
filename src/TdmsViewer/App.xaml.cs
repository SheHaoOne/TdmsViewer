using System.IO;
using System.Windows;
using TdmsViewer.Controls;
using TdmsViewer.ViewModels;
using TdmsViewer.Views;

namespace TdmsViewer;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ScottPlotFontSetup.EnsureInitialized();

        _mainViewModel = new MainViewModel();
        var window = new MainWindow { DataContext = _mainViewModel };
        MainWindow = window;
        window.Show();

        var paths = ResolveTdmsPathsFromArgs(e.Args);
        if (paths.Count > 0)
            _mainViewModel.ImportFilesFromPaths(paths, replaceSession: true);
    }

    /// <summary>
    /// 从命令行参数解析全部 .tdms 路径（支持双击关联与拖放启动）。
    /// </summary>
    internal static IReadOnlyList<string> ResolveTdmsPathsFromArgs(string[] args)
    {
        if (args.Length == 0)
            return Array.Empty<string>();

        var paths = new List<string>();
        foreach (var arg in args)
        {
            var path = arg.Trim('"');
            if (File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".tdms", StringComparison.OrdinalIgnoreCase))
                paths.Add(Path.GetFullPath(path));
        }

        return paths;
    }
}
