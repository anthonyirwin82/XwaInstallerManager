namespace XwaInstallerLibCore.Platform;

/// <summary>
/// Reads the X-Wing Alliance install path from the Windows registry.
/// On non-Windows platforms this always returns <c>null</c>.
/// </summary>
public static class RegistryHelpers
{
    private const string XwaRegistryKey = @"SOFTWARE\LucasArts Entertainment Company LLC\X-Wing Alliance\v1.0";
    private const string InstallPathValue = "Install Path";

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string? ReadXwaInstallPath()
    {
        if (!PlatformInfo.IsWindows)
        {
            return null;
        }

        try
        {
            using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                Microsoft.Win32.RegistryHive.LocalMachine,
                Microsoft.Win32.RegistryView.Registry32);

            using var key = baseKey.OpenSubKey(XwaRegistryKey);

            if (key is null)
            {
                return null;
            }

            return key.GetValue(InstallPathValue) as string;
        }
        catch
        {
            return null;
        }
    }
}
