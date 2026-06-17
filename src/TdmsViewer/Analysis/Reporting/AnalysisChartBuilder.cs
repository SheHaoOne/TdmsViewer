using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Steps;

namespace TdmsViewer.Analysis.Reporting;

internal static class AnalysisChartBuilder
{
    public static IReadOnlyList<LineSeriesData> BuildWaveformSeries(
        IReadOnlyList<AnalysisSourceSample> sources,
        int maxPoints,
        AnalysisTimeRange? globalRange,
        IReadOnlyDictionary<string, object?>? stepParameters)
    {
        var series = new List<LineSeriesData>(sources.Count);
        foreach (var source in sources)
        {
            var slice = StepSignalHelper.SliceSource(source, globalRange, stepParameters);
            var (xs, ys) = PlotDataHelper.DownsampleSeries(slice.Samples, source.SampleRateHz, maxPoints);
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
