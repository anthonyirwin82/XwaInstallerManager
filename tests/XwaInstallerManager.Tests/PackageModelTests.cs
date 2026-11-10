using Xunit;

namespace XwaInstallerManager.Tests;

/// <summary>
/// Covers the package classification logic that drives the grid's Type column
/// and the install flow (full / update / add-on / patch detection, base name
/// extraction and prerequisite display).
/// </summary>
public class PackageModelTests
{
    [Theory]
    [InlineData("XWAU2025_Full_1.0.0.zip", true, false, false)]
    [InlineData("XWAU2025_UPD_1.1.0.zip", false, true, false)]
    [InlineData("XWAU2025_AddOn-C3PO_2.0.0.zip", false, false, true)]
    [InlineData("SomePatch_1.0.zip", false, false, false)]
    public void ClassifiesPackageType(string fileName, bool full, bool update, bool addOn)
    {
        var package = new XwaPackage("https://example.com/" + fileName, null);

        Assert.Equal(full, package.IsFullPackage);
        Assert.Equal(update, package.IsUpdatePackage);
        Assert.Equal(addOn, package.IsAddOnPackage);
        Assert.Equal(!full && !update && !addOn, package.IsPatchPackageVisibility);
    }

    [Fact]
    public void PackageBaseName_StripsVersionSuffix()
    {
        var package = new XwaPackage("https://example.com/XWAU2025_Full_1.0.0.zip", null);
        Assert.Equal("XWAU2025", package.PackageBaseName);
    }

    [Fact]
    public void PackageFileName_PrefersLocalPath()
    {
        var package = new XwaPackage("https://example.com/remote.zip", "/tmp/local.zip");
        Assert.Equal("local.zip", package.PackageFileName);
    }

    [Fact]
    public void Prerequisite_DefaultsToXwaWhenEmpty()
    {
        var package = new XwaPackage("https://example.com/a.zip", null)
        {
            ModVersion = new XwaModVersion { PrerequisiteModId = string.Empty }
        };

        Assert.Equal("XWA", package.PackageViewPrerequisite);
        Assert.False(package.HasPrerequisite);
    }

    [Fact]
    public void Prerequisite_ShowsModAndVersion()
    {
        var package = new XwaPackage("https://example.com/a.zip", null)
        {
            ModVersion = new XwaModVersion { PrerequisiteModId = "XWAU2025", PrerequisiteVersion = "1.0.0" }
        };

        Assert.Equal("XWAU2025 1.0.0", package.PackageViewPrerequisiteWithVersion);
        Assert.True(package.HasPrerequisite);
    }

    [Fact]
    public void DownloadState_DrivesButtonVisibility()
    {
        var package = new XwaPackage("https://example.com/a.zip", null);

        package.DownloadVisibilityState = XwaPackage.DownloadState.Download;
        Assert.True(package.DownloadButtonVisibility);
        Assert.False(package.RedownloadButtonVisibility);
        Assert.False(package.DownloadCancelButtonVisibility);

        package.DownloadVisibilityState = XwaPackage.DownloadState.Downloading;
        Assert.True(package.DownloadCancelButtonVisibility);
        Assert.False(package.DownloadButtonVisibility);
    }

    [Fact]
    public void CheckValidPackage_RequiresScriptAndPackageJson()
    {
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string valid = Path.Combine(dir, "valid.zip");
            using (var archive = new System.IO.Compression.ZipArchive(File.Create(valid), System.IO.Compression.ZipArchiveMode.Create))
            {
                archive.CreateEntry("script.nsi");
                archive.CreateEntry("Package.json");
            }

            string invalid = Path.Combine(dir, "invalid.zip");
            using (var archive = new System.IO.Compression.ZipArchive(File.Create(invalid), System.IO.Compression.ZipArchiveMode.Create))
            {
                archive.CreateEntry("script.nsi");
            }

            Assert.True(new XwaPackage(null, valid).CheckValidPackage());
            Assert.False(new XwaPackage(null, invalid).CheckValidPackage());
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void AddOrUpdatePackage_MergesUrlAndPathByFileName()
    {
        var packages = new XwaPackages();
        string dir = TestPaths.CreateTempDirectory();

        try
        {
            string local = Path.Combine(dir, "mod.zip");
            File.WriteAllBytes(local, Array.Empty<byte>());

            packages.AddOrUpdatePackage("https://example.com/mod.zip", null, null, null, null);
            packages.AddOrUpdatePackage(null, local, null, null, null);

            Assert.Single(packages.Packages);
            Assert.True(packages.Packages[0].HasPackageUrl);
            Assert.True(packages.Packages[0].HasPackagePath);
        }
        finally
        {
            TestPaths.TryDelete(dir);
        }
    }

    [Fact]
    public void AddOrUpdatePackage_RejectsMalformedUrl()
    {
        var packages = new XwaPackages();
        Assert.False(packages.AddOrUpdatePackage("not a url", null, null, null, null));
        Assert.Empty(packages.Packages);
    }
}
