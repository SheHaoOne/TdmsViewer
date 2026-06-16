using System.IO;
using NvhLibCSharp;

namespace TdmsViewer.Services;

public static class NvhLicenseService
{
    private static bool _loaded;
    private static string? _loadedPath;
    private static string? _lastError;

    public static bool IsLoaded => _loaded;

    public static string? LastError => _lastError;

    public static bool TryLoad(string? licensePath = null)
    {
        if (_loaded)
            return true;

        var candidates = BuildCandidatePaths(licensePath);
        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                Nvh.LoadLicense(candidate);
                _loaded = true;
                _loadedPath = candidate;
                _lastError = null;
                return true;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        _lastError ??= "未找到有效的 NVH 许可证文件（*.lic）。";
        return false;
    }

    public static void EnsureLoaded()
    {
        if (!TryLoad())
            throw new InvalidOperationException(_lastError ?? "NVH 许可证加载失败。");
    }

    public static string? LoadedPath => _loadedPath;

    private static IEnumerable<string> BuildCandidatePaths(string? licensePath)
    {
        if (!string.IsNullOrWhiteSpace(licensePath))
            yield return licensePath;

        var baseDir = AppContext.BaseDirectory;
        foreach (var file in Directory.EnumerateFiles(baseDir, "*.lic"))
            yield return file;

        var libDir = Path.Combine(baseDir, "Lib");
        if (Directory.Exists(libDir))
        {
            foreach (var file in Directory.EnumerateFiles(libDir, "*.lic"))
                yield return file;
        }
    }
}
