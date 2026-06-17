using System.Globalization;
using TdmsViewer.Analysis.Contracts;

namespace TdmsViewer.Analysis;

public static class HeatmapColorRangeResolver
{
    public static string? Validate(bool useAuto, double min, double max, string contextLabel = "热力图色阶")
    {
        if (useAuto)
            return null;

        if (max <= min)
            return $"{contextLabel}：上限必须大于下限。";

        return null;
    }

    public static HeatmapColorRange? Resolve(bool useAuto, double min, double max)
    {
        if (useAuto)
            return null;

        return HeatmapColorRange.CreateManual(min, max);
    }

    public static bool TryParse(string? minText, string? maxText, out double min, out double max)
    {
        min = 0;
        max = 0;
        if (!double.TryParse(minText, NumberStyles.Float, CultureInfo.InvariantCulture, out min))
            return false;

        return double.TryParse(maxText, NumberStyles.Float, CultureInfo.InvariantCulture, out max);
    }
}
