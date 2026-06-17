namespace TdmsViewer.Analysis.Reporting;

internal static class PlotDataHelper
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
        {
            Array.Resize(ref xs, index);
            Array.Resize(ref ys, index);
        }

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

    public static double[] BuildFrequencyAxis(int bins, double deltaTime, int spectrumLines)
    {
        var resolution = 1.0 / (deltaTime * spectrumLines * 2);
        var axis = new double[bins];
        for (var i = 0; i < bins; i++)
            axis[i] = i * resolution;

        return axis;
    }

    public static int ResolveSpectrumLines(double deltaTime, string calcType, double calcValue)
    {
        return calcType switch
        {
            "SpectrumLines" => (int)calcValue,
            "Resolution" => (int)Math.Round(1.0 / (deltaTime * calcValue * 2)),
            "FrameLength" => (int)Math.Round(calcValue / 2),
            _ => (int)calcValue
        };
    }

    public static string FormatFrequencyLabel(double hz) =>
        hz >= 1000 ? $"{hz / 1000:0.#}k" : hz.ToString("0");

    public static (double Min, double Max) GetMatrixRange(double[,] values)
    {
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        var found = false;

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                var value = values[i, j];
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                found = true;
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }
        }

        if (!found)
            return (0, 1);

        if (min >= max)
            max = min + 1;

        return (min, max);
    }

    public static double[] ToLog10Axis(double[] values) =>
        values.Select(v => v > 0 ? Math.Log10(v) : double.NaN).ToArray();

    public static double InterpolateAlongAxis(double[] axis, double[] values, double target)
    {
        if (axis.Length == 0 || values.Length == 0)
            return double.NaN;

        if (target <= axis[0])
            return values[0];

        if (target >= axis[^1])
            return values[^1];

        for (var i = 0; i < axis.Length - 1; i++)
        {
            if (target < axis[i + 1])
            {
                var span = axis[i + 1] - axis[i];
                if (span <= 0)
                    return values[i];

                var ratio = (target - axis[i]) / span;
                return values[i] + (values[i + 1] - values[i]) * ratio;
            }
        }

        return values[^1];
    }

    public static (double[,] Values, double[] FrequencyAxis) ResampleFrequencyRowsToLinearGrid(
        double[,] values,
        double[] sourceFrequencyAxis,
        double minFrequencyHz,
        double maxFrequencyHz,
        int targetFrequencyBins)
    {
        var targetFrequencyAxis = NvhLibCSharp.Utils.MathUtils
            .Linspace(minFrequencyHz, maxFrequencyHz, targetFrequencyBins)
            .ToArray();
        var timeBins = values.GetLength(1);
        var result = new double[targetFrequencyBins, timeBins];

        for (var t = 0; t < timeBins; t++)
        {
            var column = new double[sourceFrequencyAxis.Length];
            for (var f = 0; f < sourceFrequencyAxis.Length; f++)
                column[f] = values[f, t];

            for (var i = 0; i < targetFrequencyBins; i++)
                result[i, t] = InterpolateAlongAxis(sourceFrequencyAxis, column, targetFrequencyAxis[i]);
        }

        return (result, targetFrequencyAxis);
    }
}
