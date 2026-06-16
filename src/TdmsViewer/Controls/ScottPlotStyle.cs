using ScottPlot;

namespace TdmsViewer.Controls;

internal static class ScottPlotStyle
{
    private static readonly Color GridColor = Color.FromHex("#D2D2D7");
    private static readonly Color PlotBgColor = Color.FromHex("#E8E8ED");

    public static void ApplyMacTheme(Plot plot)
    {
        ScottPlotFontSetup.Apply(plot);

        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = PlotBgColor;
        plot.Grid.MajorLineColor = GridColor.WithAlpha(0.6);
        plot.Grid.MinorLineColor = Colors.Transparent;
        plot.Axes.Color(GridColor);
    }
}
