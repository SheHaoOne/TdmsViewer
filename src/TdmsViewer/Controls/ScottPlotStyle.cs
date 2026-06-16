using ScottPlot;

namespace TdmsViewer.Controls;

internal static class ScottPlotStyle
{
    private static readonly Color GridColor = Color.FromHex("#D2D2D7");
    private static readonly Color AxisTitleColor = Color.FromHex("#1D1D1F");
    private static readonly Color AxisTickColor = Color.FromHex("#515154");

    private const float AxisTitleFontSize = 13f;
    private const float AxisTickFontSize = 12f;

    public static void ApplyMacTheme(Plot plot)
    {
        ScottPlotFontSetup.Apply(plot);

        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = Colors.Transparent;
        plot.Grid.MajorLineColor = GridColor.WithAlpha(0.6);
        plot.Grid.MinorLineColor = Colors.Transparent;
        plot.Axes.FrameColor(GridColor);

        ApplyAxisTypography(plot.Axes.Bottom);
        ApplyAxisTypography(plot.Axes.Left);
        ApplyAxisTypography(plot.Axes.Top);
        ApplyAxisTypography(plot.Axes.Right);
    }

    private static void ApplyAxisTypography(IAxis axis)
    {
        axis.Label.FontSize = AxisTitleFontSize;
        axis.Label.ForeColor = AxisTitleColor;
        axis.Label.Bold = false;

        axis.TickLabelStyle.FontSize = AxisTickFontSize;
        axis.TickLabelStyle.ForeColor = AxisTickColor;

        axis.MajorTickStyle.Color = GridColor;
        axis.MinorTickStyle.Color = GridColor.WithAlpha(0.6);
    }
}
