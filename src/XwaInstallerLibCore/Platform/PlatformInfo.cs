using System.Runtime.InteropServices;

namespace XwaInstallerLibCore.Platform;

/// <summary>
/// Cross-platform helpers that replace the Windows-only P/Invoke and registry
/// calls used by the original WPF implementation.
/// </summary>
public static class PlatformInfo
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Formats a byte count using the same style as the Windows
    /// <c>Shlwapi!StrFormatByteSize</c> function (e.g. "1.50 MB").
    /// </summary>
    public static string FormatByteSize(long fileSize)
    {
        if (fileSize < 0)
        {
            fileSize = 0;
        }

        if (fileSize < 1024)
        {
            return $"{fileSize} bytes";
        }

        double value = fileSize;
        string[] units = { "KB", "MB", "GB", "TB", "PB" };
        int unit = -1;

        do
        {
            value /= 1024.0;
            unit++;
        }
        while (value >= 1024.0 && unit < units.Length - 1);

        return value >= 100.0
            ? $"{value:0} {units[unit]}"
            : $"{value:0.00} {units[unit]}";
    }

    /// <summary>
    /// Returns the directory that contains the running executable.
    /// </summary>
    public static string GetExecutableDirectory()
    {
        string? path = GetExecutablePath();

        if (string.IsNullOrEmpty(path))
        {
            return AppContext.BaseDirectory;
        }

        return Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;
    }

    /// <summary>
    /// Returns the full path of the running executable.
    /// </summary>
    /// <remarks>
    /// <see cref="Environment.ProcessPath"/> is the reliable source and works in
    /// single-file publishes. <c>Assembly.Location</c> is deliberately not used:
    /// it returns an empty string for assemblies embedded in a single-file app.
    /// </remarks>
    public static string GetExecutablePath()
    {
        string? path = Environment.ProcessPath;

        if (string.IsNullOrEmpty(path))
        {
            // Single-file publishes have no assembly location; fall back to the
            // app directory plus the process name.
            string name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs().FirstOrDefault() ?? string.Empty);

            if (!string.IsNullOrEmpty(name))
            {
                string candidate = Path.Combine(AppContext.BaseDirectory, name);

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return AppContext.BaseDirectory;
        }

        return path;
    }

    /// <summary>
    /// Returns the user's desktop directory, or the home directory when no
    /// desktop folder can be resolved (common on headless Linux).
    /// </summary>
    public static string GetDesktopDirectory()
    {
        string? desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
        {
            return desktop;
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrEmpty(home) ? AppContext.BaseDirectory : home;
    }
}
