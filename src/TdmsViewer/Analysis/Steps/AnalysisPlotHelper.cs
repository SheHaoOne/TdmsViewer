using NVHAlgorithmKit.FrequencyDomain;
using NVHAlgorithmKit.Transform;

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

    public static (string[] TimeLabels, string[] FreqLabels, object[] HeatmapData) BuildStftHeatmap(
        StftResult stft,
        int maxTimeFrames = 80,
        int maxFreqBins = 64)
    {
        var frameCount = stft.TimeAxis.Length;
        var freqCount = stft.Frequencies.Length;
        if (frameCount == 0 || freqCount == 0)
            return (Array.Empty<string>(), Array.Empty<string>(), Array.Empty<object>());

        var timeStep = Math.Max(1, frameCount / maxTimeFrames);
        var freqStep = Math.Max(1, freqCount / maxFreqBins);

        var timeLabels = new List<string>();
        var freqLabels = new List<string>();
        var data = new List<object>();

        for (var fi = 0; fi < freqCount; fi += freqStep)
            freqLabels.Add(FormatFrequency(stft.Frequencies[fi]));

        var timeIndex = 0;
        for (var ti = 0; ti < frameCount; ti += timeStep)
        {
            timeLabels.Add(stft.TimeAxis[ti].ToString("G4"));
            var freqIndex = 0;
            for (var fi = 0; fi < freqCount; fi += freqStep)
            {
                var magnitude = stft.Magnitude[ti, fi];
                if (magnitude > 0)
                    data.Add(new object[] { timeIndex, freqIndex, magnitude });

                freqIndex++;
            }

            timeIndex++;
        }

        return (timeLabels.ToArray(), freqLabels.ToArray(), data.ToArray());
    }

    public static (string[] TimeLabels, string[] FreqLabels, object[] HeatmapData) BuildCwtHeatmap(
        CwtResult cwt,
        int maxTimeFrames = 80,
        int maxFreqBins = 64)
    {
        var frameCount = cwt.TimeAxis.Length;
        var freqCount = cwt.Frequencies.Length;
        if (frameCount == 0 || freqCount == 0)
            return (Array.Empty<string>(), Array.Empty<string>(), Array.Empty<object>());

        var timeStep = Math.Max(1, frameCount / maxTimeFrames);
        var freqStep = Math.Max(1, freqCount / maxFreqBins);

        var timeLabels = new List<string>();
        var freqLabels = new List<string>();
        var data = new List<object>();

        for (var fi = 0; fi < freqCount; fi += freqStep)
            freqLabels.Add(FormatFrequency(cwt.Frequencies[fi]));

        var timeIndex = 0;
        for (var ti = 0; ti < frameCount; ti += timeStep)
        {
            timeLabels.Add(cwt.TimeAxis[ti].ToString("G4"));
            var freqIndex = 0;
            for (var fi = 0; fi < freqCount; fi += freqStep)
            {
                var magnitude = cwt.Magnitude[fi, ti];
                if (magnitude > 0)
                    data.Add(new object[] { timeIndex, freqIndex, magnitude });

                freqIndex++;
            }

            timeIndex++;
        }

        return (timeLabels.ToArray(), freqLabels.ToArray(), data.ToArray());
    }

    private static string FormatFrequency(double hz) =>
        hz >= 1000 ? $"{hz / 1000:0.#}k" : hz.ToString("0");
}
