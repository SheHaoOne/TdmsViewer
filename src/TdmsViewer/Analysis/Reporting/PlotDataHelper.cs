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
        hz >= 1000 ? $"{hz / 1000:0.#}k" : hz < 10 ? hz.ToString("0.##") : hz.ToString("0");

    public static IReadOnlyList<double> BuildLogFrequencyTickValues(double minHz, double maxHz, int maxTicks = 7)
    {
        if (minHz <= 0 || maxHz <= minHz || maxTicks < 2)
            return Array.Empty<double>();

        var candidates = CollectDecadeTicks(minHz, maxHz, [1, 2, 5]);
        if (candidates.Count <= maxTicks)
            return candidates;

        candidates = CollectDecadeTicks(minHz, maxHz, [1]);
        if (candidates.Count <= maxTicks)
            return candidates;

        var selected = new List<double>(maxTicks);
        var step = (double)(candidates.Count - 1) / (maxTicks - 1);
        for (var i = 0; i < maxTicks; i++)
        {
            var index = (int)Math.Round(i * step);
            selected.Add(candidates[Math.Clamp(index, 0, candidates.Count - 1)]);
        }

        return selected.Distinct().OrderBy(hz => hz).ToArray();
    }

    private static List<double> CollectDecadeTicks(double minHz, double maxHz, int[] multipliers)
    {
        var minExp = (int)Math.Floor(Math.Log10(minHz));
        var maxExp = (int)Math.Ceiling(Math.Log10(maxHz));
        var ticks = new List<double>();

        for (var exp = minExp; exp <= maxExp; exp++)
        {
            var decade = Math.Pow(10, exp);
            foreach (var multiplier in multipliers)
            {
                var hz = multiplier * decade;
                if (hz >= minHz && hz <= maxHz)
                    ticks.Add(hz);
            }
        }

        return ticks.Distinct().OrderBy(hz => hz).ToList();
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
