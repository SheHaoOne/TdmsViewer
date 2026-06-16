using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdmsViewer.Analysis.Reporting;

public static class ReportSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string ToJson(AnalysisReport report) =>
        JsonSerializer.Serialize(report, Options);

    public static async Task ExportHtmlAsync(AnalysisReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var json = ToJson(report);
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Dashboard", "Assets", "dashboard.html");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("未找到报表模板 dashboard.html", templatePath);

        var template = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var html = template.Replace("/*__REPORT_JSON__*/null", json, StringComparison.Ordinal);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(filePath, html, cancellationToken);
    }
}
