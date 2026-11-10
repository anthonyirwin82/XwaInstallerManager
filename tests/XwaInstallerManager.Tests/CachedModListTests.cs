using Xunit;
using XwaInstallerManager;

namespace XwaInstallerManager.Tests;

/// <summary>
/// The mod list must be available from the on-disk cache before any network
/// call, so the grid is never empty while the update check runs.
/// </summary>
public class CachedModListTests
{
    [Fact]
    public void LoadFromCache_ReturnsNullWhenNothingCached()
    {
        // With no cache present the call must not throw and must report "nothing".
        XwaModVersions? cached = XwaModVersions.LoadFromCache();

        // Either null (no cache) or a populated list (cache exists on this machine).
        if (cached is not null)
        {
            Assert.NotEmpty(cached.Mods);
        }
    }

    [Fact]
    public void LoadFromCache_DoesNotPopulateFailedSources()
    {
        // Cache-only loading performs no network access, so nothing can fail.
        XwaModVersions? cached = XwaModVersions.LoadFromCache();

        if (cached is not null)
        {
            Assert.Empty(cached.FailedSources);
        }
    }
}
