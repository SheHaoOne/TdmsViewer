using System.Globalization;
using System.Text.Json;

namespace TdmsViewer.Analysis.Pipeline;

internal static class AnalysisPlanParameterConverter
{
    public static Dictionary<string, object?> FromJsonElements(Dictionary<string, JsonElement>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return new Dictionary<string, object?>(StringComparer.Ordinal);

        return parameters.ToDictionary(
            pair => pair.Key,
            pair => ConvertJsonElement(pair.Value),
            StringComparer.Ordinal);
    }

    private static object? ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };

    public static object? NormalizeLoadedValue(object? value)
    {
        if (value is not JsonElement element)
            return value;

        return ConvertJsonElement(element);
    }
}
