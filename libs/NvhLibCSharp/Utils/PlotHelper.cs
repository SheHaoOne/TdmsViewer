using ScottPlot;

namespace NvhLibCSharp.Utils;

public static class PlotHelper
{
    private static readonly Color[] _colormap =
    [
        new Color("#002589"),
        new Color("#0025b9"),
        new Color("#0025fe"),
        new Color("#0056fe"),
        new Color("#0077fe"),
        new Color("#009bfe"),
        new Color("#00bcfe"),
        new Color("#00dffe"),
        new Color("#00d5c8"),
        new Color("#00c395"),
        new Color("#00bc6f"),
        new Color("#00bc6f"),
        new Color("#00c346"),
        new Color("#00d837"),
        new Color("#32e711"),
        new Color("#95dd2a"),
        new Color("#c1e711"),
        new Color("#e5f111"),
        new Color("#e5f111"),
        new Color("#fbfb44"),
        new Color("#f4b700"),
        new Color("#f47f00"),
        new Color("#fb3232")
    ];

    public static void PlotFigure(string figurePath, double[] xAxis, List<(double[] Ys, string Legend)> series, int width = 1920, int height = 1080)
    {
        var dir = Path.GetDirectoryName(figurePath)
            ?? throw new IOException($"Cannot find a path from ${figurePath}");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var plt = new Plot();

        foreach (var (y, legend) in series)
        {
            var signal = plt.Add.SignalXY(xAxis, y);
            signal.LegendText = legend;
            signal.Color.WithOpacity(0.375);
        }

        plt.SavePng(figurePath, width, height);
    }

    public static void PlotFigure(string figurePath, List<(double[] Xs, double[] Ys, string Legend)> series, int width = 1920, int height = 1080, bool displayLegend = true)
    {
        var dir = Path.GetDirectoryName(figurePath)
            ?? throw new IOException($"Cannot find a path from ${figurePath}");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var plt = new Plot();
        foreach (var (xs, ys, legend) in series)
        {
            var signal = plt.Add.SignalXY(xs, ys);
            signal.LegendText = legend;
            signal.Color.WithOpacity(0.375);
        }
        plt.Legend.IsVisible = displayLegend;
        plt.SavePng(figurePath, width, height);
    }

    public static void PlotColormap(string figurePath, double[,] data, double[] xAxis, double[] yAxis, double yMin, double yMax, int width = 1920, int height = 1080)
    {
        var dir = Path.GetDirectoryName(figurePath)
            ?? throw new IOException($"Cannot find a path from ${figurePath}");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var plt = new Plot();
        var hm = plt.Add.Heatmap(data);
        hm.Smooth = true;

        hm.FlipVertically = true;
        hm.Rectangle = new CoordinateRect(xAxis[0], xAxis[^1], yAxis[0], yAxis[^1]);

        hm.Colormap = new ScottPlot.Colormaps.CustomPalette(_colormap);
        hm.ManualRange = new ScottPlot.Range(yMin, yMax);
        plt.Add.ColorBar(hm);

        plt.SavePng(figurePath, width, height);
    }
}
