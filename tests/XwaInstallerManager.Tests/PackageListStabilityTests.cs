using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// The package list must not be cleared while a refresh is in flight: doing so
/// makes the cached rows vanish and reappear during the download.
/// </summary>
public class PackageListStabilityTests
{
    [Fact]
    public void AddOrUpdatePackage_MergesByUrlWithoutDuplicating()
    {
        var packages = new XwaPackages();
        const string url = "https://example.com/mod_Full_1.0.0.zip";

        // First add (as Refresh() does, without mod data).
        packages.AddOrUpdatePackage(url, null, null, null, null);
        Assert.Single(packages.Packages);

        // Second add with mod data (as the fetch does) must update, not append.
        var mod = new XwaMod { ModId = "TEST", Name = "Test" };
        var version = new XwaModVersion { Version = "1.0.0" };
        packages.AddOrUpdatePackage(url, null, null, mod, version);

        Assert.Single(packages.Packages);
        Assert.Equal("1.0.0", packages.Packages[0].PackageViewVersion);
        Assert.Equal("Test", packages.Packages[0].PackageViewName);
    }

    [Fact]
    public void AddOrUpdatePackage_KeepsExistingInstanceWhenMerging()
    {
        var packages = new XwaPackages();
        const string url = "https://example.com/mod_Full_1.0.0.zip";

        packages.AddOrUpdatePackage(url, null, null, null, null);
        XwaPackage original = packages.Packages[0];

        packages.AddOrUpdatePackage(url, null, null, new XwaMod { ModId = "TEST" }, new XwaModVersion { Version = "2.0.0" });

        // The same instance must be updated in place, so the grid row is not
        // replaced (which would cause a visible redraw).
        Assert.Same(original, packages.Packages[0]);
    }

    [Fact]
    public void AddOrUpdatePackage_KeepsDistinctModsSharingOneUrl()
    {
        // Regression: a custom source can point at the same release asset as a
        // built-in mod. Matching by URL alone merged them, so the second mod
        // overwrote the first and the built-in one disappeared from the list.
        var packages = new XwaPackages();
        const string url = "https://github.com/example/releases/download/V1.0.0/tool-linux-amd64";

        var builtIn = new XwaMod { ModId = "XWAPILOT", Name = "Built-in" };
        var custom = new XwaMod { ModId = "NEWAAP1", Name = "Custom" };

        packages.AddOrUpdatePackage(url, null, null, builtIn, new XwaModVersion { Version = "1.0.0" });
        packages.AddOrUpdatePackage(url, null, null, custom, new XwaModVersion { Version = "1.0.0" });

        Assert.Equal(2, packages.Packages.Count);
        Assert.Contains(packages.Packages, p => p.Mod?.ModId == "XWAPILOT");
        Assert.Contains(packages.Packages, p => p.Mod?.ModId == "NEWAAP1");
    }

    [Fact]
    public void AddOrUpdatePackage_StillMergesSameModSameUrl()
    {
        // The same mod added twice (e.g. cached load then network refresh) must
        // still merge into one row.
        var packages = new XwaPackages();
        const string url = "https://example.com/mod_Full_1.0.0.zip";
        var mod = new XwaMod { ModId = "TEST" };

        packages.AddOrUpdatePackage(url, null, null, mod, new XwaModVersion { Version = "1.0.0" });
        packages.AddOrUpdatePackage(url, null, null, mod, new XwaModVersion { Version = "1.0.0" });

        Assert.Single(packages.Packages);
    }

    [Fact]
    public void BatchObservableCollection_AddRangeRaisesSingleReset()
    {
        var collection = new BatchObservableCollection<string>();
        int notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;

        collection.AddRange(new[] { "a", "b", "c" });

        Assert.Equal(3, collection.Count);
        Assert.Equal(1, notifications);
    }
}
