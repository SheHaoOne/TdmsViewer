using ScottPlot;

namespace TdmsViewer.Controls;

internal static class ScottPlotFontSetup
{
    private static bool _initialized;
    private static string _cjkFont = Fonts.Sans;

    public static string CjkFontName
    {
        get
        {
            EnsureInitialized();
            return _cjkFont;
        }
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        _cjkFont = ResolveCjkFont();
        Fonts.Default = _cjkFont;
    }

    public static void Apply(Plot plot)
    {
        EnsureInitialized();
        plot.Font.Set(_cjkFont);
    }

    private static string ResolveCjkFont()
    {
        const string probe = "中文频谱分析";
        var detected = Fonts.Detect(probe);
        if (!string.IsNullOrWhiteSpace(detected) && FontExists(detected))
            return detected;

        foreach (var candidate in new[]
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "PingFang SC",
            "SimHei",
            "Segoe UI"
        })
        {
            if (FontExists(candidate))
                return candidate;
        }

        return Fonts.Sans;
    }

    private static bool FontExists(string name) =>
        Fonts.GetTypeface(name, bold: false, italic: false) is not null;
}
