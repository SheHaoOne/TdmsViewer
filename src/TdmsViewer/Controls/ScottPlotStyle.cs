using ScottPlot;

namespace TdmsViewer.Controls;

internal static class ScottPlotStyle
{
    private static readonly Color GridColor = Color.FromHex("#D2D2D7");

    public static void ApplyMacTheme(Plot plot)
    {
        ScottPlotFontSetup.Apply(plot);

        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = Colors.Transparent;
        plot.Grid.MajorLineColor = GridColor.WithAlpha(0.6);
        plot.Grid.MinorLineColor = Colors.Transparent;
        plot.Axes.Color(GridColor);
    }
}
