using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TdmsViewer.Analysis.Parameters;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisParameterItem : ObservableObject
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    [ObservableProperty]
    private string? _description;

    public required AnalysisParameterKind Kind { get; init; }
    public IReadOnlyList<AnalysisChoiceOption> ChoiceOptions { get; init; } = Array.Empty<AnalysisChoiceOption>();

    [ObservableProperty]
    private string _textValue = string.Empty;

    [ObservableProperty]
    private string? _selectedChoice;

    public bool IsChoice => Kind == AnalysisParameterKind.Choice;

    public bool IsNumeric => Kind is AnalysisParameterKind.Integer or AnalysisParameterKind.Double;

    public bool IsText => Kind == AnalysisParameterKind.Text;

    public bool IsEditable => Kind != AnalysisParameterKind.Choice;

    public static AnalysisParameterItem FromDefinition(AnalysisParameterDefinition definition)
    {
        var item = new AnalysisParameterItem
        {
            Key = definition.Key,
            DisplayName = definition.DisplayName,
            Description = definition.Description,
            Kind = definition.Kind,
            ChoiceOptions = definition.Choices
        };
        item.SetFromObject(definition.DefaultValue);
        return item;
    }

    public void UpdateDescription(string? description) => Description = description;

    public void SetFromObject(object? value)
    {
        value = NormalizeValue(value);

        if (Kind == AnalysisParameterKind.Choice)
        {
            var text = value?.ToString();
            SelectedChoice = ChoiceOptions.FirstOrDefault(c => string.Equals(c.Value, text, StringComparison.OrdinalIgnoreCase))?.Value
                             ?? ChoiceOptions.FirstOrDefault()?.Value;
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
            return SelectedChoice ?? ChoiceOptions.FirstOrDefault()?.Value;

        if (Kind == AnalysisParameterKind.Text)
            return string.IsNullOrWhiteSpace(TextValue) ? null : TextValue.Trim();

        if (Kind == AnalysisParameterKind.Integer)
        {
            return int.TryParse(TextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        return double.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    public string? Validate(string stepDisplayName)
    {
        if (Kind == AnalysisParameterKind.Choice)
        {
            if (string.IsNullOrWhiteSpace(SelectedChoice))
                return $"{stepDisplayName} · {DisplayName}：请选择一个选项。";

            if (ChoiceOptions.All(c => !string.Equals(c.Value, SelectedChoice, StringComparison.OrdinalIgnoreCase)))
                return $"{stepDisplayName} · {DisplayName}：选项无效。";

            return null;
        }

        if (Kind == AnalysisParameterKind.Text)
        {
            if (string.IsNullOrWhiteSpace(TextValue))
                return $"{stepDisplayName} · {DisplayName}：请输入文本。";

            return null;
        }

        if (Kind == AnalysisParameterKind.Integer)
        {
            if (!int.TryParse(TextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
                return $"{stepDisplayName} · {DisplayName}：请输入大于 0 的整数。";

            return null;
        }

        if (!double.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return $"{stepDisplayName} · {DisplayName}：请输入有效数值。";

        return Key switch
        {
            "overlap" when number is < 0 or > 1 => $"{stepDisplayName} · {DisplayName}：请输入 0~1 之间的数值。",
            "referenceValue" when number <= 0 => $"{stepDisplayName} · {DisplayName}：请输入大于 0 的数值。",
            "increment" or "stepValue" when number <= 0 => $"{stepDisplayName} · {DisplayName}：请输入大于 0 的数值。",
            "maxPoints" when number <= 0 => $"{stepDisplayName} · {DisplayName}：请输入大于 0 的整数。",
            _ => null
        };
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
