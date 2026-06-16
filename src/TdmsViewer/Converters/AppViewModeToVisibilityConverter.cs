using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Converters;

public sealed class AppViewModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppViewMode current)
            return Visibility.Collapsed;

        var targetText = parameter switch
        {
            string s => s,
            AppViewMode mode => mode.ToString(),
            _ => parameter?.ToString()
        };

        if (string.IsNullOrWhiteSpace(targetText) ||
            !Enum.TryParse<AppViewMode>(targetText, ignoreCase: true, out var target))
        {
            return Visibility.Collapsed;
        }

        return current == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
