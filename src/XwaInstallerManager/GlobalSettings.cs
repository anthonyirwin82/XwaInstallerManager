using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using DnsClient;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public static class GlobalSettings
{
    public static readonly Encoding Encoding = GetEncoding("iso-8859-1");

    public static readonly IPEndPoint AlternateEndPoint = NameServer.GooglePublicDns2;

    public static readonly string XwaInstallerManagerDirectory = PlatformInfo.GetExecutableDirectory();

    public static readonly string XwaInstallerManagerRepository = "https://github.com/anthonyirwin82/XwaInstallerManager";

    /// <summary>
    /// Plain-text file containing the latest version (one line, e.g. "1.0.0").
    /// </summary>
    public static readonly string XwaInstallerManagerUpdateUrl =
        XwaInstallerManagerRepository + "/raw/main/version.txt";

    /// <summary>
    /// Release asset name for the current platform, as produced by the release
    /// workflow (see .github/workflows/release.yml).
    /// </summary>
    public static string XwaInstallerManagerAssetName
    {
        get
        {
            if (PlatformInfo.IsWindows)
            {
                return "XwaInstallerManager-win-x64.zip";
            }

            if (PlatformInfo.IsMacOS)
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? "XwaInstallerManager-macos-arm64.zip"
                    : "XwaInstallerManager-macos-x64.zip";
            }

            return "XwaInstallerManager-linux-x64.zip";
        }
    }

    /// <summary>
    /// Download URL of the release asset for the current platform.
    /// </summary>
    public static string XwaInstallerManagerSetupUrl =>
        XwaInstallerManagerRepository + "/releases/latest/download/" + XwaInstallerManagerAssetName;

    public static readonly string XwaInstallerPackagesUrl = "https://xwaupgrade.com/version/mod-versions.json";

    /// <summary>
    /// Additional mod packs merged into the list (appended after the main list).
    /// </summary>
    public static readonly string[] AdditionalModPackUrls =
    {
        "https://raw.githubusercontent.com/anthonyirwin82/xwa-pilot/refs/heads/main/xwa-pilot-versions.json"
    };

    public static string InstallerPackagesFolder => Path.Combine(XwaInstallerManagerDirectory, "InstallerPackages");

    public static string DownloadPackagesListPath => Path.Combine(InstallerPackagesFolder, "DownloadPackages.txt");

    public static HttpClient CreateHttpClient(HttpMessageHandler? handler)
    {
        var httpClient = handler is not null ? new HttpClient(handler) : new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        httpClient.Timeout = TimeSpan.FromMinutes(5.0);
        return httpClient;
    }

    private static Encoding GetEncoding(string name)
    {
        XwaInstallerLibCore.Platform.EncodingSetup.EnsureRegistered();
        return System.Text.Encoding.GetEncoding(name);
    }
}
