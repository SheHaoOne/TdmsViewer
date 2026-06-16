using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TdmsViewer.Analysis.Parameters;

namespace TdmsViewer.ViewModels;

public sealed partial class AnalysisStepItem : ObservableObject
{
    public required string StepType { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }

    [ObservableProperty]
    private bool _isEnabled;

    public ObservableCollection<AnalysisParameterItem> Parameters { get; } = new();

    public bool HasParameters => Parameters.Count > 0;

    public void InitializeParameters()
    {
        Parameters.Clear();
        foreach (var definition in AnalysisStepParameterCatalog.Get(StepType))
            Parameters.Add(AnalysisParameterItem.FromDefinition(definition));

        if (string.Equals(StepType, "AveragedSpectrum", StringComparison.OrdinalIgnoreCase))
        {
            var calcType = Parameters.FirstOrDefault(p => p.Key == "calcType");
            var stepType = Parameters.FirstOrDefault(p => p.Key == "stepType");
            if (calcType != null)
                calcType.PropertyChanged += (_, _) => RefreshContextualDescriptions();
            if (stepType != null)
                stepType.PropertyChanged += (_, _) => RefreshContextualDescriptions();
            RefreshContextualDescriptions();
        }
    }

    private void RefreshContextualDescriptions()
    {
        if (!string.Equals(StepType, "AveragedSpectrum", StringComparison.OrdinalIgnoreCase))
            return;

        var calcType = Parameters.FirstOrDefault(p => p.Key == "calcType")?.SelectedChoice;
        var stepType = Parameters.FirstOrDefault(p => p.Key == "stepType")?.SelectedChoice;

        Parameters.FirstOrDefault(p => p.Key == "calcValue")
            ?.UpdateDescription(AnalysisStepParameterCatalog.GetCalcValueDescription(calcType));
        Parameters.FirstOrDefault(p => p.Key == "stepValue")
            ?.UpdateDescription(AnalysisStepParameterCatalog.GetStepValueDescription(stepType));
    }

    public void ApplyParameterValues(IReadOnlyDictionary<string, object?>? values)
    {
        var defaults = AnalysisStepParameterCatalog.GetDefaults(StepType);
        foreach (var parameter in Parameters)
        {
            if (values != null && values.TryGetValue(parameter.Key, out var value) && value != null)
                parameter.SetFromObject(value);
            else if (defaults.TryGetValue(parameter.Key, out var fallback))
                parameter.SetFromObject(fallback);
        }

        RefreshContextualDescriptions();
    }

    public Dictionary<string, object?> ToParameterDictionary() =>
        Parameters.ToDictionary(p => p.Key, p => p.ToObject(), StringComparer.Ordinal);

    public string? ValidateParameters()
    {
        foreach (var parameter in Parameters)
        {
            var error = parameter.Validate(DisplayName);
            if (error != null)
                return error;
        }

        if (string.Equals(StepType, "AveragedSpectrum", StringComparison.OrdinalIgnoreCase))
        {
            var stepType = Parameters.FirstOrDefault(p => p.Key == "stepType")?.SelectedChoice;
            var stepValue = Parameters.FirstOrDefault(p => p.Key == "stepValue");
            if (string.Equals(stepType, "Overlap", StringComparison.OrdinalIgnoreCase)
                && stepValue != null
                && double.TryParse(stepValue.TextValue, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var overlap)
                && overlap is < 0 or > 1)
            {
                return $"{DisplayName} · {stepValue.DisplayName}：请输入 0~1 之间的数值。";
            }
        }

        return null;
    }
}
