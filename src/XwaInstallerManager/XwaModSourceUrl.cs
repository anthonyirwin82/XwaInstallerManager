namespace XwaInstallerManager;

public sealed class XwaModSourceUrl
{
    public string SourceName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional platform this source applies to: "windows", "linux" or "mac".
    /// Empty means the source is platform-independent (the default).
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// True when this source applies to the current platform.
    /// </summary>
    public bool MatchesCurrentPlatform()
    {
        if (string.IsNullOrEmpty(Platform))
        {
            return true;
        }

        return Platform.Trim().ToLowerInvariant() switch
        {
            "windows" or "win" => XwaInstallerLibCore.Platform.PlatformInfo.IsWindows,
            "linux" => XwaInstallerLibCore.Platform.PlatformInfo.IsLinux,
            "mac" or "macos" or "osx" => XwaInstallerLibCore.Platform.PlatformInfo.IsMacOS,
            _ => true
        };
    }
}
