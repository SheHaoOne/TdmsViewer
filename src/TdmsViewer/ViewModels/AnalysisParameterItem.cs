using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TdmsViewer.Analysis.Parameters;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisParameterItem : ObservableObject
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required AnalysisParameterKind Kind { get; init; }
    public IReadOnlyList<string> Choices { get; init; } = Array.Empty<string>();

    [ObservableProperty]
    private string _textValue = string.Empty;

    [ObservableProperty]
    private string? _selectedChoice;

    public bool IsChoice => Kind == AnalysisParameterKind.Choice;

    public bool IsNumeric => Kind != AnalysisParameterKind.Choice;

    public static AnalysisParameterItem FromDefinition(AnalysisParameterDefinition definition)
    {
        var item = new AnalysisParameterItem
        {
            Key = definition.Key,
            DisplayName = definition.DisplayName,
            Description = definition.Description,
            Kind = definition.Kind,
            Choices = definition.Choices
        };
        item.SetFromObject(definition.DefaultValue);
        return item;
    }

    public void SetFromObject(object? value)
    {
        value = NormalizeValue(value);

        if (Kind == AnalysisParameterKind.Choice)
        {
            SelectedChoice = value?.ToString() ?? Choices.FirstOrDefault();
            return;
        }

        TextValue = value switch
        {
            double d => d.ToString("G", CultureInfo.InvariantCulture),
            float f => f.ToString("G", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object? ToObject()
    {
        if (Kind == AnalysisParameterKind.Choice)
            return SelectedChoice ?? Choices.FirstOrDefault();

        if (Kind == AnalysisParameterKind.Integer)
        {
            return int.TryParse(TextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        return double.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0.0;
    }

    private static object? NormalizeValue(object? value) =>
        value switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } json when json.TryGetInt64(out var integer) => integer,
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetDouble(),
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            JsonElement json => json.ToString(),
            _ => value
        };
}
