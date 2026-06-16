using System.Globalization;
using System.Windows.Data;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Converters;

public sealed class AppViewModeEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppViewMode current || parameter is not string targetText)
            return false;

        return Enum.TryParse<AppViewMode>(targetText, ignoreCase: true, out var target) && current == target;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string targetText &&
            Enum.TryParse<AppViewMode>(targetText, ignoreCase: true, out var target))
        {
            return target;
        }

        return AppViewMode.Browse;
    }
}
