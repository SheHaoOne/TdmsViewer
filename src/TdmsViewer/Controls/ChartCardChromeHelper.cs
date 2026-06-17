using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TdmsViewer.Controls;

internal static class ChartCardChromeHelper
{
    public static void EnterFullscreenMode(Border cardBorder, params UIElement[] chromeToHide)
    {
        cardBorder.ClearValue(FrameworkElement.StyleProperty);
        cardBorder.Background = Brushes.Transparent;
        cardBorder.BorderBrush = Brushes.Transparent;
        cardBorder.BorderThickness = new Thickness(0);
        cardBorder.Effect = null;
        cardBorder.Padding = new Thickness(0);
        cardBorder.Margin = new Thickness(0);

        foreach (var element in chromeToHide)
            element.Visibility = Visibility.Collapsed;
    }
}
