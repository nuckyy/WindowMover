using Microsoft.Win32;

namespace WindowMover;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowMover";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value
            && string.Equals(value, GetCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, GetCommand(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string GetCommand() => $"\"{Application.ExecutablePath}\"";
}
