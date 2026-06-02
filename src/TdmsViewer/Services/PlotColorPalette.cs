namespace TdmsViewer.Services;

public static class PlotColorPalette
{
    private static readonly string[] Colors =
    [
        "#007AFF",
        "#FF9500",
        "#34C759",
        "#FF3B30",
        "#AF52DE",
        "#5856D6",
        "#00C7BE",
        "#FF2D55",
        "#A2845E",
        "#8E8E93"
    ];

    public static string GetColor(int index) => Colors[index % Colors.Length];
}
