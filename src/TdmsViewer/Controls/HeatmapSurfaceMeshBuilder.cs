using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ScottPlot;
using ScottPlot.Colormaps;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Controls;

internal static class HeatmapSurfaceMeshBuilder
{
    private const int MaxMeshDimension = 96;

    public static GeometryModel3D BuildSurface(
        HeatmapChartModel model,
        double colorMin,
        double colorMax,
        out Rect3D bounds)
    {
        var (values, xAxis, yAxis) = DownsampleGrid(model.Values, model.XAxis, model.YAxis, MaxMeshDimension);
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);

        var xMin = xAxis[0];
        var xMax = xAxis[^1];
        var yAxisValues = model.UseLogYAxis ? PlotDataHelper.ToLog10Axis(yAxis) : yAxis;
        var yMin = yAxisValues[0];
        var yMax = yAxisValues[^1];
        var xSpan = Math.Max(xMax - xMin, 1e-9);
        var ySpan = Math.Max(yMax - yMin, 1e-9);
        var zSpan = Math.Max(colorMax - colorMin, 1e-9);

        var positions = new Point3DCollection(rows * cols);
        var textureCoordinates = new PointCollection(rows * cols);
        var indices = new Int32Collection((rows - 1) * (cols - 1) * 6);

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;

        for (var row = 0; row < rows; row++)
        {
            var yCoord = yAxisValues[row];
            var normalizedY = (yCoord - yMin) / ySpan;

            for (var col = 0; col < cols; col++)
            {
                var normalizedX = (xAxis[col] - xMin) / xSpan;
                var value = values[row, col];
                var normalizedZ = double.IsNaN(value) || double.IsInfinity(value)
                    ? 0
                    : (value - colorMin) / zSpan;

                var point = new Point3D(normalizedX, normalizedZ, normalizedY);
                positions.Add(point);
                textureCoordinates.Add(new Point(
                    cols <= 1 ? 0 : col / (double)(cols - 1),
                    rows <= 1 ? 0 : 1 - row / (double)(rows - 1)));

                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }
        }

        for (var row = 0; row < rows - 1; row++)
        {
            for (var col = 0; col < cols - 1; col++)
            {
                var topLeft = row * cols + col;
                var topRight = topLeft + 1;
                var bottomLeft = topLeft + cols;
                var bottomRight = bottomLeft + 1;

                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(topRight);
                indices.Add(topRight);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
            }
        }

        var mesh = new MeshGeometry3D
        {
            Positions = positions,
            TextureCoordinates = textureCoordinates,
            TriangleIndices = indices
        };

        var bitmap = BuildColormapBitmap(values, colorMin, colorMax);
        var material = new DiffuseMaterial(new ImageBrush(bitmap)
        {
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.Fill
        });

        bounds = new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
        return new GeometryModel3D(mesh, material) { BackMaterial = material };
    }

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

    private static BitmapSource BuildColormapBitmap(double[,] values, double min, double max)
    {
        var rows = values.GetLength(0);
        var cols = values.GetLength(1);
        var pixels = new byte[rows * cols * 4];
        var colormap = new Turbo();
        var span = Math.Max(max - min, 1e-9);

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var value = values[row, col];
                var fraction = double.IsNaN(value) || double.IsInfinity(value)
                    ? 0
                    : Math.Clamp((value - min) / span, 0, 1);
                var color = colormap.GetColor(fraction);
                var offset = (row * cols + col) * 4;
                pixels[offset] = color.Blue;
                pixels[offset + 1] = color.Green;
                pixels[offset + 2] = color.Red;
                pixels[offset + 3] = 255;
            }
        }

        var stride = cols * 4;
        return BitmapSource.Create(cols, rows, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }
}
