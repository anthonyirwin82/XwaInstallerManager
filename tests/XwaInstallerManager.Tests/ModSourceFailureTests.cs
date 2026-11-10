using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// A mod source that cannot be loaded must be reported, not silently skipped.
/// </summary>
public class ModSourceFailureTests
{
    [Fact]
    public void FailedSources_IsEmptyForValidJson()
    {
        var versions = new XwaModVersions();

        Assert.Empty(versions.FailedSources);
    }

    [Fact]
    public void FailedSources_IsNotSerialized()
    {
        // FailedSources is a runtime diagnostic; it must not round-trip into JSON.
        var versions = new XwaModVersions();
        versions.FailedSources.Add("https://example.com/x.json: could not be downloaded");

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(versions);

        Assert.DoesNotContain("FailedSources", json);
    }
}
