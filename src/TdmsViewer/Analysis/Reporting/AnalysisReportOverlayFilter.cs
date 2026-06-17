namespace TdmsViewer.Analysis.Reporting;

internal static class AnalysisReportOverlayFilter
{
    public static IReadOnlyList<ChartCardModel> Filter(
        IReadOnlyList<ChartCardModel> cards,
        IReadOnlySet<string> visibleSourceKeys)
    {
        var result = new List<ChartCardModel>(cards.Count);

        foreach (var card in cards)
        {
            switch (card)
            {
                case LineChartModel line:
                {
                    var series = line.Series
                        .Where(s => IsVisible(s.SourceKey, visibleSourceKeys))
                        .ToList();
                    if (series.Count == 0)
                        continue;

                    result.Add(line with { Series = series });
                    break;
                }
                case BarChartModel bar when bar.Series is { Count: > 0 } overlaySeries:
                {
                    var series = overlaySeries
                        .Where(s => IsVisible(s.SourceKey, visibleSourceKeys))
                        .ToList();
                    if (series.Count == 0)
                        continue;

                    result.Add(bar with { Series = series, Values = series[0].Values });
                    break;
                }
                case BarChartModel singleBar:
                {
                    if (!IsVisible(singleBar.SourceKey, visibleSourceKeys))
                        continue;

                    result.Add(singleBar);
                    break;
                }
                default:
                    result.Add(card);
                    break;
            }
        }

        return result;
    }

    private static bool IsVisible(string? sourceKey, IReadOnlySet<string> visibleSourceKeys) =>
        string.IsNullOrWhiteSpace(sourceKey) || visibleSourceKeys.Contains(sourceKey);
}
