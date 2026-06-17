using System.Text.Json;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

internal static class HeatmapEChartsPayloadBuilder
{
    private const int MaxMeshDimension = 80;

    public static string BuildJson(HeatmapChartModel model, double colorMin, double colorMax)
    {
        var (values, xAxis, yAxis) = DownsampleGrid(model.Values, model.XAxis, model.YAxis, MaxMeshDimension);
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);
        var yAxisValues = model.UseLogYAxis ? PlotDataHelper.ToLog10Axis(yAxis) : yAxis;
        var data = new List<double[]>(rows * cols);

        for (var row = 0; row < rows; row++)
        {
            var y = yAxisValues[row];
            if (double.IsNaN(y) || double.IsInfinity(y))
                continue;

            for (var col = 0; col < cols; col++)
            {
                var value = values[row, col];
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                data.Add(
                [
                    xAxis[col],
                    y,
                    value
                ]);
            }
        }

        var payload = new HeatmapSurface3DPayload
        {
            XLabel = model.XLabel,
            YLabel = model.YLabel,
            ZLabel = "幅值",
            ColorMin = colorMin,
            ColorMax = colorMax,
            UseLogYAxis = model.UseLogYAxis,
            Data = data
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static (double[,] Values, double[] XAxis, double[] YAxis) DownsampleGrid(
        double[,] values,
        double[] xAxis,
        double[] yAxis,
        int maxDimension)
    {
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);
        if (rows <= maxDimension && cols <= maxDimension)
            return (values, xAxis, yAxis);

        var rowStep = Math.Max(1, (int)Math.Ceiling(rows / (double)maxDimension));
        var colStep = Math.Max(1, (int)Math.Ceiling(cols / (double)maxDimension));
        var targetRows = (rows + rowStep - 1) / rowStep;
        var targetCols = (cols + colStep - 1) / colStep;

        var downsampled = new double[targetRows, targetCols];
        var downsampledX = new double[targetCols];
        var downsampledY = new double[targetRows];

        var targetRow = 0;
        for (var row = 0; row < rows; row += rowStep, targetRow++)
        {
            downsampledY[targetRow] = yAxis[Math.Min(row, yAxis.Length - 1)];
            var targetCol = 0;
            for (var col = 0; col < cols; col += colStep, targetCol++)
            {
                if (targetRow == 0)
                    downsampledX[targetCol] = xAxis[Math.Min(col, xAxis.Length - 1)];

                downsampled[targetRow, targetCol] = values[row, col];
            }
        }

        return (downsampled, downsampledX, downsampledY);
    }

    private sealed class HeatmapSurface3DPayload
    {
        public required string XLabel { get; init; }

        public required string YLabel { get; init; }

        public required string ZLabel { get; init; }

        public double ColorMin { get; init; }

        public double ColorMax { get; init; }

        public bool UseLogYAxis { get; init; }

        public required List<double[]> Data { get; init; }
    }
}
