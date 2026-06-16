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
    }

    public Dictionary<string, object?> ToParameterDictionary() =>
        Parameters.ToDictionary(p => p.Key, p => p.ToObject(), StringComparer.Ordinal);
}
