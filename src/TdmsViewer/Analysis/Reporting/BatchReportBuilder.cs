using System.Text.Json;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Reporting;

public static class BatchReportBuilder
{
    public static AnalysisReport BuildCompareReport(
        IReadOnlyList<AnalysisReport> reports,
        string planName,
        string channelDisplayName)
    {
        if (reports.Count == 0)
            throw new ArgumentException("至少需要一个分析报表。", nameof(reports));

        var columns = new List<string> { "文件" };
        var metricKeys = new List<(string BlockId, string Label, string Format)>
        {
            ("spl", "SPL (A) dB", "F1"),
            ("td", "RMS", "G4"),
            ("td", "峰值", "G4")
        };

        foreach (var metric in metricKeys)
            columns.Add(metric.Label);

        var rows = new List<object[]>();
        foreach (var report in reports)
        {
            var row = new object[columns.Count];
            row[0] = report.Meta.FileName;

            for (var i = 0; i < metricKeys.Count; i++)
            {
                var metric = metricKeys[i];
                row[i + 1] = ExtractMetric(report, metric.BlockId, metric.Label);
            }

            rows.Add(row);
        }

        var compareBlock = new ReportBlock
        {
            BlockId = "compare",
            WidgetType = "CompareTable",
            Title = "多文件指标对比",
            Payload = new { columns, rows }
        };

        var layout = DefaultDashboardLayout.ForTemplate("batch-compare-light", ["compare"]);

        return new AnalysisReport
        {
            Meta = new ReportMeta
            {
                Title = $"{planName} — 批量对比",
                FileName = $"{reports.Count} 个文件",
                ChannelName = channelDisplayName,
                GroupName = reports[0].Meta.GroupName,
                SampleRateHz = reports[0].Meta.SampleRateHz,
                SampleCount = reports.Max(r => r.Meta.SampleCount),
                GeneratedAt = DateTime.Now,
                PlanName = planName
            },
            Blocks = [compareBlock],
            Layout = layout
        };
    }

    private static object ExtractMetric(AnalysisReport report, string blockId, string label)
    {
        var block = report.Blocks.FirstOrDefault(b =>
            string.Equals(b.BlockId, blockId, StringComparison.OrdinalIgnoreCase));

        if (block == null)
            return "—";

        try
        {
            var json = JsonSerializer.Serialize(block.Payload);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (string.Equals(block.WidgetType, "KpiCard", StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("value", out var kpiValue))
            {
                return kpiValue.GetDouble();
            }

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("label", out var itemLabel) &&
                        string.Equals(itemLabel.GetString(), label, StringComparison.OrdinalIgnoreCase) &&
                        item.TryGetProperty("value", out var itemValue))
                    {
                        return itemValue.GetDouble();
                    }
                }
            }
        }
        catch
        {
            // fall through
        }

        return "—";
    }
}
