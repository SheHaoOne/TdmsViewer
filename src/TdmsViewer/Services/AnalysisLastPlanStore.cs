using System.IO;
using TdmsViewer.Analysis.Pipeline;

namespace TdmsViewer.Services;

/// <summary>
/// 持久化上一次运行分析时使用的方案，供下次打开分析工作台时恢复。
/// </summary>
public static class AnalysisLastPlanStore
{
    private static string PlanFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TdmsViewer",
        "last-analysis-plan.json");

    public static AnalysisPlan? TryLoad()
    {
        try
        {
            var path = PlanFilePath;
            if (!File.Exists(path))
                return null;

            return AnalysisPlanSerializer.LoadAsync(path).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveAsync(AnalysisPlan plan, CancellationToken cancellationToken = default)
    {
        var path = PlanFilePath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await AnalysisPlanSerializer.SaveAsync(plan, path, cancellationToken);
    }
}
