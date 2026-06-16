using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TdmsViewer.ViewModels;

namespace TdmsViewer.Converters;

public sealed class AppViewModeToVisibilityConverter : IValueConverter
{
    public AppViewMode TargetMode { get; set; } = AppViewMode.Viewer;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var mode = value is AppViewMode viewMode ? viewMode : AppViewMode.Viewer;
        return mode == TargetMode ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
