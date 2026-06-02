using System.Globalization;
using System.Windows.Data;

namespace TdmsViewer.Converters;

/// <summary>
/// 将父元素实际高度按参数比例（如 0.333）转换为 MaxHeight 等数值。
/// </summary>
public sealed class HeightFractionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double height || height <= 0)
            return double.PositiveInfinity;

        var fraction = 1.0 / 3.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) && f > 0)
            fraction = f;

        return Math.Max(0, height * fraction);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
