using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisPlanDocument
{
    public string Name { get; set; } = "快速声学检查";
    public List<AnalysisPlanStepDocument> Steps { get; set; } = new();
}

public sealed class AnalysisPlanStepDocument
{
    public required string Id { get; set; }
    public required string StepType { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, JsonElement>? Parameters { get; set; }
}

public static class AnalysisPlanSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SaveAsync(AnalysisPlan plan, string filePath, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(plan);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, Options, cancellationToken);
    }

    public static async Task<AnalysisPlan> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<AnalysisPlanDocument>(stream, Options, cancellationToken)
            ?? throw new InvalidOperationException("方案文件为空。");

        return FromDocument(document);
    }

    public static AnalysisPlanDocument ToDocument(AnalysisPlan plan) => new()
    {
        Name = plan.Name,
        Steps = plan.Steps.Select(step => new AnalysisPlanStepDocument
        {
            Id = step.Id,
            StepType = step.StepType,
            Enabled = step.Enabled,
            Parameters = step.Parameters.Count == 0
                ? null
                : step.Parameters.ToDictionary(
                    pair => pair.Key,
                    pair => JsonSerializer.SerializeToElement(pair.Value, Options))
        }).ToList()
    };

    public static AnalysisPlan FromDocument(AnalysisPlanDocument document) => new()
    {
        Name = document.Name,
        Steps = document.Steps.Select(step => new AnalysisPlanStep
        {
            Id = step.Id,
            StepType = step.StepType,
            Enabled = step.Enabled,
            Parameters = AnalysisPlanParameterConverter.FromJsonElements(step.Parameters)
        }).ToList()
    };
}
