using Microsoft.Win32;

namespace DesktopGarden;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LovelyPlants";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (enabled)
        {
            key?.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key?.DeleteValue(ValueName, false);
        }
    }
}
