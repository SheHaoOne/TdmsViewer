using System.Windows;
using System.Windows.Input;

namespace TdmsViewer.Controls;

public partial class ChartFullscreenWindow : Window
{
    public ChartFullscreenWindow(UIElement content, string title)
    {
        InitializeComponent();
        Title = title;
        Host.Content = content;
        Owner = Application.Current.MainWindow;
        ChartFullscreenService.ApplyFullscreenLayout(content);
        Loaded += (_, _) => ChartFullscreenService.RefreshChartContent(content);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
