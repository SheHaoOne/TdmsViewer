using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TdmsViewer.Analysis.Contracts;

namespace TdmsViewer.Analysis.Pipeline;

public sealed class AnalysisPlanDocument
{
    public string Name { get; set; } = "快速声学检查";
    public AnalysisTimeRangeDocument? GlobalTimeRange { get; set; }
    public HeatmapColorRangeDocument? GlobalHeatmapColorRange { get; set; }
    public List<AnalysisPlanStepDocument> Steps { get; set; } = new();
}

public sealed class AnalysisTimeRangeDocument
{
    public double StartSec { get; set; }
    public double? EndSec { get; set; }
}

public sealed class HeatmapColorRangeDocument
{
    public bool UseAuto { get; set; } = true;
    public double Min { get; set; }
    public double Max { get; set; }
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
        GlobalTimeRange = plan.GlobalTimeRange == null
            ? null
            : new AnalysisTimeRangeDocument
            {
                StartSec = plan.GlobalTimeRange.StartSec,
                EndSec = plan.GlobalTimeRange.EndSec
            },
        GlobalHeatmapColorRange = plan.GlobalHeatmapColorRange == null
            ? null
            : new HeatmapColorRangeDocument
            {
                UseAuto = plan.GlobalHeatmapColorRange.UseAuto,
                Min = plan.GlobalHeatmapColorRange.Min,
                Max = plan.GlobalHeatmapColorRange.Max
            },
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
        GlobalTimeRange = document.GlobalTimeRange == null
            ? null
            : new AnalysisTimeRange
            {
                StartSec = document.GlobalTimeRange.StartSec,
                EndSec = document.GlobalTimeRange.EndSec
            },
        GlobalHeatmapColorRange = document.GlobalHeatmapColorRange == null
            ? null
            : new HeatmapColorRange
            {
                UseAuto = document.GlobalHeatmapColorRange.UseAuto,
                Min = document.GlobalHeatmapColorRange.Min,
                Max = document.GlobalHeatmapColorRange.Max
            },
        Steps = document.Steps.Select(step => new AnalysisPlanStep
        {
            Id = step.Id,
            StepType = step.StepType,
            Enabled = step.Enabled,
            Parameters = MigrateParameters(AnalysisPlanParameterConverter.FromJsonElements(step.Parameters))
        }).ToList()
    };

    private static Dictionary<string, object?> MigrateParameters(Dictionary<string, object?> parameters)
    {
        if (parameters.TryGetValue("skipInSec", out var skipValue) && skipValue != null)
        {
            if (!parameters.ContainsKey("startTimeSec")
                && double.TryParse(skipValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var skip))
            {
                parameters["startTimeSec"] = skip;
            }

            parameters.Remove("skipInSec");
        }

        return parameters;
    }
}
