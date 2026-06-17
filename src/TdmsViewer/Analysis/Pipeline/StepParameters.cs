using System.Globalization;
using System.Text.Json;

namespace TdmsViewer.Analysis.Pipeline;

internal static class StepParameters
{
    public static int GetInt(IReadOnlyDictionary<string, object?>? parameters, string key, int defaultValue)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
            return defaultValue;

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetInt32(),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    public static double GetDouble(IReadOnlyDictionary<string, object?>? parameters, string key, double defaultValue)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
            return defaultValue;

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetDouble(),
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    public static string GetString(IReadOnlyDictionary<string, object?>? parameters, string key, string defaultValue)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
            return defaultValue;

        return value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? defaultValue,
            _ => value.ToString() ?? defaultValue
        };
    }
}
