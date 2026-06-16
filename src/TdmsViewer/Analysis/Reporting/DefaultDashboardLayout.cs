namespace TdmsViewer.Analysis.Reporting;

public static class DefaultDashboardLayout
{
    public static DashboardLayout ForTemplate(string templateId, IReadOnlyCollection<string> blockIds)
    {
        if (string.Equals(templateId, "nvh-acoustic-light", StringComparison.OrdinalIgnoreCase))
            return AcousticLight(blockIds);

        return AutoGrid(blockIds);
    }

    private static DashboardLayout AcousticLight(IReadOnlyCollection<string> blockIds)
    {
        var widgets = new List<WidgetPlacement>();
        var idSet = blockIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (idSet.Contains("spl"))
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = "spl",
                WidgetType = "KpiCard",
                Col = 0,
                Row = 0,
                ColSpan = 6,
                RowSpan = 2
            });
        }

        if (idSet.Contains("td"))
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = "td",
                WidgetType = "KpiGroup",
                Col = 6,
                Row = 0,
                ColSpan = 18,
                RowSpan = 2
            });
        }

        if (idSet.Contains("wf"))
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = "wf",
                WidgetType = "LineChart",
                Col = 0,
                Row = 2,
                ColSpan = 24,
                RowSpan = 5
            });
        }

        if (idSet.Contains("sp"))
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = "sp",
                WidgetType = "LineChart",
                Col = 0,
                Row = 7,
                ColSpan = 12,
                RowSpan = 6
            });
        }

        if (idSet.Contains("ob"))
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = "ob",
                WidgetType = "BarChart",
                Col = 12,
                Row = 7,
                ColSpan = 12,
                RowSpan = 6
            });
        }

        return new DashboardLayout { Columns = 24, Widgets = widgets };
    }

    private static DashboardLayout AutoGrid(IReadOnlyCollection<string> blockIds)
    {
        var widgets = new List<WidgetPlacement>();
        var col = 0;
        var row = 0;

        foreach (var id in blockIds)
        {
            widgets.Add(new WidgetPlacement
            {
                BlockId = id,
                WidgetType = "Auto",
                Col = col,
                Row = row,
                ColSpan = 12,
                RowSpan = 4
            });

            col += 12;
            if (col >= 24)
            {
                col = 0;
                row += 4;
            }
        }

        return new DashboardLayout { Columns = 24, Widgets = widgets };
    }
}
