using System.Windows;
using System.Windows.Input;

namespace TdmsViewer.Controls;

public partial class ChartFullscreenWindow : Window
{
    public ChartFullscreenWindow(UIElement content, string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Title = title;
        Host.Content = content;
        Owner = Application.Current.MainWindow;
        ChartFullscreenService.ApplyFullscreenLayout(content);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
