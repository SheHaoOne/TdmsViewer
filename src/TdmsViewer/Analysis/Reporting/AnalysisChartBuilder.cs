using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Reporting;

internal static class AnalysisChartBuilder
{
    public static IReadOnlyList<LineSeriesData> BuildWaveformSeries(
        IReadOnlyList<AnalysisSourceSample> sources,
        int maxPoints = 2000)
    {
        var series = new List<LineSeriesData>(sources.Count);
        foreach (var source in sources)
        {
            var (xs, ys) = PlotDataHelper.DownsampleSeries(source.Samples, source.SampleRateHz, maxPoints);
            series.Add(new LineSeriesData
            {
                Label = source.Label,
                X = xs,
                Y = ys,
                Color = source.Color,
                SourceKey = source.FilePath
            });
        }

        return series;
    }
}
