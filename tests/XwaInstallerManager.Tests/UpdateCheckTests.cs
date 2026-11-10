using Semver;
using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// The update check reads a plain-text version file and downloads the release
/// asset matching the current platform.
/// </summary>
public class UpdateCheckTests
{
    [Fact]
    public void AssetName_MatchesCurrentPlatform()
    {
        string asset = GlobalSettings.XwaInstallerManagerAssetName;

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("XwaInstallerManager-win-x64.zip", asset);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Assert.StartsWith("XwaInstallerManager-macos-", asset);
        }
        else
        {
            Assert.Equal("XwaInstallerManager-linux-x64.zip", asset);
        }
    }

    [Fact]
    public void AssetName_UsesMacosNotOsx()
    {
        // The release workflow names macOS archives "macos-*", not "osx-*".
        Assert.DoesNotContain("osx", GlobalSettings.XwaInstallerManagerAssetName);
    }

    [Fact]
    public void SetupUrl_PointsAtLatestReleaseAsset()
    {
        string url = GlobalSettings.XwaInstallerManagerSetupUrl;

        Assert.Contains("github.com/anthonyirwin82/XwaInstallerManager", url);
        Assert.Contains("/releases/latest/download/", url);
        Assert.EndsWith(GlobalSettings.XwaInstallerManagerAssetName, url);
    }

    [Fact]
    public void UpdateUrl_PointsAtVersionFile()
    {
        Assert.EndsWith("/raw/main/version.txt", GlobalSettings.XwaInstallerManagerUpdateUrl);
    }

    [Theory]
    [InlineData("1.0.0", false)]
    [InlineData("1.0.1", true)]
    [InlineData("1.1.0", true)]
    [InlineData("2.0.0", true)]
    public void NewerRemoteVersion_TriggersUpdate(string remoteText, bool expected)
    {
        var local = SemVersion.Parse("1.0.0", SemVersionStyles.Any);
        var remote = SemVersion.Parse(remoteText, SemVersionStyles.Any);

        Assert.Equal(expected, local.CompareSortOrderTo(remote) < 0);
    }
}
