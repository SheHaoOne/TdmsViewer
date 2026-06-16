using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisPlanDocument
{
    public int Version { get; set; } = 1;

    public string Name { get; set; } = "NVH 声学分析";

    public string DashboardTemplateId { get; set; } = "nvh-acoustic-light";

    public bool BatchMode { get; set; }

    public List<AnalysisPlanStepDocument> Steps { get; set; } = new();
}

public sealed class AnalysisPlanStepDocument
{
    public string StepType { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

public static class AnalysisPlanSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SaveAsync(AnalysisPlanDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(document, Options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public static async Task<AnalysisPlanDocument> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var document = JsonSerializer.Deserialize<AnalysisPlanDocument>(json, Options)
                       ?? throw new InvalidDataException("分析方案文件为空或格式无效。");

        if (document.Steps.Count == 0)
            throw new InvalidDataException("分析方案未包含任何步骤。");

        return document;
    }
}
