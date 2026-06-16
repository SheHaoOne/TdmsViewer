namespace TdmsViewer.Analysis.Steps;

internal static class AnalysisPlotHelper
{
    public static (double[] X, double[] Y) DownsampleSeries(double[] samples, double sampleRateHz, int maxPoints = 2000)
    {
        if (samples.Length == 0)
            return (Array.Empty<double>(), Array.Empty<double>());

        var step = Math.Max(1, samples.Length / maxPoints);
        var count = (samples.Length + step - 1) / step;
        var xs = new double[count];
        var ys = new double[count];
        var dt = 1.0 / sampleRateHz;
        var index = 0;

        for (var i = 0; i < samples.Length; i += step)
        {
            xs[index] = i * dt;
            ys[index] = samples[i];
            index++;
        }

        if (index < count)
            Array.Resize(ref xs, index);

        if (index < count)
            Array.Resize(ref ys, index);

        return (xs, ys);
    }

    public static (double[] X, double[] Y) DownsampleXY(double[] x, double[] y, int maxPoints = 2000)
    {
        if (x.Length == 0 || y.Length == 0)
            return (Array.Empty<double>(), Array.Empty<double>());

        var length = Math.Min(x.Length, y.Length);
        var step = Math.Max(1, length / maxPoints);
        var count = (length + step - 1) / step;
        var xs = new double[count];
        var ys = new double[count];
        var index = 0;

        for (var i = 0; i < length; i += step)
        {
            xs[index] = x[i];
            ys[index] = y[i];
            index++;
        }

        if (index < count)
        {
            Array.Resize(ref xs, index);
            Array.Resize(ref ys, index);
        }

        return (xs, ys);
    }
}
