using ScottPlot;

namespace NvhLibCSharp.Utils;

public static class PlotHelper
{
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

        hm.Colormap = new ScottPlot.Colormaps.Turbo();
        hm.ManualRange = new ScottPlot.Range(yMin, yMax);
        plt.Add.ColorBar(hm);

        plt.SavePng(figurePath, width, height);
    }
}
