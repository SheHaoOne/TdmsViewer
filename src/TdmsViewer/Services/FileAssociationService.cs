using Microsoft.Win32;

namespace TdmsViewer.Services;

/// <summary>
/// 注册 / 取消注册 .tdms 文件与当前应用程序的关联（需用户权限写入注册表）。
/// </summary>
public static class FileAssociationService
{
    private const string ProgId = "TdmsViewer.Document";
    private const string Extension = ".tdms";

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}\shell\open\command");
        if (key?.GetValue(null) is not string command)
            return false;

        var exe = GetExecutablePath();
        return command.Contains(exe, StringComparison.OrdinalIgnoreCase);
    }

    public static void Register()
    {
        var exe = GetExecutablePath();
        var quotedExe = $"\"{exe}\"";

        using (var ext = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}"))
            ext.SetValue(null, ProgId);

        using (var prog = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            prog.SetValue(null, "TDMS 数据文件");

        using (var icon = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            icon.SetValue(null, $"{quotedExe},0");

        using (var open = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            open.SetValue(null, $"{quotedExe} \"%1\"");

        NativeMethods.AssociateApplication(exe);
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Extension}", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false);
    }

    private static string GetExecutablePath() =>
        Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
}

internal static class NativeMethods
{
    public static void AssociateApplication(string exePath)
    {
        try
        {
            var appName = "TdmsViewer";
            using var apps = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications");
            using var appKey = apps.CreateSubKey(appName);
            using var cmd = appKey.CreateSubKey(@"shell\open\command");
            cmd.SetValue(null, $"\"{exePath}\" \"%1\"");
        }
        catch
        {
            // 非关键路径
        }
    }
}
