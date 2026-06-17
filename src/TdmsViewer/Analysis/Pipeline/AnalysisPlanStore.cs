using System.IO;

namespace TdmsViewer.Analysis.Pipeline;

public static class AnalysisPlanStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TdmsViewer",
        "last-analysis-plan.json");

    public static AnalysisPlan? TryLoadLastPlan()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            return AnalysisPlanSerializer.LoadAsync(FilePath).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveLastPlanAsync(AnalysisPlan plan, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await AnalysisPlanSerializer.SaveAsync(plan, FilePath, cancellationToken);
    }
}
