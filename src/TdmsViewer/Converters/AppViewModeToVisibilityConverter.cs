using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Converters;

public sealed class AppViewModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppViewMode current || parameter is not string targetText)
            return Visibility.Collapsed;

        if (!Enum.TryParse<AppViewMode>(targetText, ignoreCase: true, out var target))
            return Visibility.Collapsed;

        return current == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
