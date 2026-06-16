using System.IO;
using NvhLibCSharp;

namespace TdmsViewer.Services;

public static class NvhLicenseService
{
    /// <summary>
    /// 与 NvhLibCSharp 项目一同部署到输出目录的许可证文件名。
    /// </summary>
    public const string LicenseFileName = "license.lic";

    private static bool _loaded;
    private static string? _lastError;

    public static bool IsLoaded => _loaded;

    public static string? LastError => _lastError;

    public static string LicensePath => Path.Combine(AppContext.BaseDirectory, LicenseFileName);

    public static bool TryLoad()
    {
        if (_loaded)
            return true;

        var licensePath = LicensePath;
        if (!File.Exists(licensePath))
        {
            _lastError = $"未找到许可证文件：{licensePath}";
            return false;
        }

        try
        {
            Nvh.LoadLicense(licensePath);
            _loaded = true;
            _lastError = null;
            return true;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            return false;
        }
    }

    public static void EnsureLoaded()
    {
        if (!TryLoad())
            throw new InvalidOperationException(_lastError ?? "NVH 许可证加载失败。");
    }
}
