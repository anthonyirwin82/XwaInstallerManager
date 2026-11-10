using System.Globalization;
using Newtonsoft.Json;

namespace XwaInstallerManager;

public sealed class XwaModVersion
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm 'UTC'";

    public string Version { get; set; } = string.Empty;

    public string InstallerFileName { get; set; } = string.Empty;

    public string InstallerFileSHA256 { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime DateUtc
    {
        get
        {
            _ = DateTime.TryParseExact(Date, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime result);
            return result;
        }
        set => Date = value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }

    public string ChangeLog { get; set; } = string.Empty;

    public List<XwaModSourceUrl> Sources { get; } = new();

    /// <summary>
    /// Returns the source that applies to the current platform, preferring a
    /// platform-specific match over a platform-independent one.
    /// </summary>
    [JsonIgnore]
    public XwaModSourceUrl? CurrentPlatformSource
    {
        get
        {
            XwaModSourceUrl? platformSpecific = Sources.FirstOrDefault(
                s => !string.IsNullOrEmpty(s.Platform) && s.MatchesCurrentPlatform());

            return platformSpecific ?? Sources.FirstOrDefault(s => string.IsNullOrEmpty(s.Platform));
        }
    }

    public bool IsBaseVersion { get; set; }

    /// <summary>
    /// When true, the download is a plain zip that is extracted into its own
    /// folder instead of being run through the NSIS installer. Use this for
    /// software that is not an XWA mod (no <c>script.nsi</c> / <c>Package.json</c>).
    /// </summary>
    public bool IsPlainZip { get; set; }

    public string PrerequisiteModId { get; set; } = string.Empty;

    public string PrerequisiteVersion { get; set; } = string.Empty;
}
