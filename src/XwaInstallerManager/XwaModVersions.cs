using Newtonsoft.Json;

namespace XwaInstallerManager;

public sealed class XwaModVersions
{
    public List<XwaMod> Mods { get; } = new();

    public List<string> ModPacks { get; } = new();

    /// <summary>
    /// Sources that could not be loaded, as "url: reason" strings. Populated by
    /// <see cref="DownloadModVersionsAsync"/> so the UI can report them.
    /// </summary>
    [JsonIgnore]
    public List<string> FailedSources { get; } = new();

    public static async Task<XwaModVersions?> DownloadModVersionsAsync(string? contentUrl, CancellationToken cancellationToken = default)
    {
        string? contentJson = await GetContentJsonAsync(contentUrl, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(contentJson))
        {
            return null;
        }

        XwaModVersions? xwaModVersions = JsonConvert.DeserializeObject<XwaModVersions>(contentJson);

        if (xwaModVersions is null)
        {
            return null;
        }

        foreach (string modPack in xwaModVersions.ModPacks)
        {
            await MergeAndRecordAsync(xwaModVersions, modPack, cancellationToken).ConfigureAwait(false);
        }

        // Built-in extra packs (e.g. community mods) are appended last so they
        // appear at the bottom of the list.
        foreach (string modPack in GlobalSettings.AdditionalModPackUrls)
        {
            await MergeAndRecordAsync(xwaModVersions, modPack, cancellationToken).ConfigureAwait(false);
        }

        // User-supplied mod list URLs, appended after everything else.
        foreach (string modPack in ManagerSettings.Default.AdditionalModSources)
        {
            await MergeAndRecordAsync(xwaModVersions, modPack, cancellationToken).ConfigureAwait(false);
        }

        return xwaModVersions;
    }

    private static async Task MergeAndRecordAsync(XwaModVersions modVersions, string modPackUrl, CancellationToken cancellationToken)
    {
        string? error = await MergeModPackAsync(modVersions, modPackUrl, cancellationToken).ConfigureAwait(false);

        if (error is not null)
        {
            modVersions.FailedSources.Add($"{modPackUrl}: {error}");
        }
    }

    /// <summary>
    /// Merges a mod pack into <paramref name="modVersions"/>.
    /// </summary>
    /// <returns>Null on success, otherwise a human-readable failure reason.</returns>
    private static async Task<string?> MergeModPackAsync(XwaModVersions modVersions, string modPackUrl, CancellationToken cancellationToken)
    {
        string? contentJson = await GetContentJsonAsync(modPackUrl, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(contentJson))
        {
            return "could not be downloaded";
        }

        XwaModVersions? pack;

        try
        {
            pack = JsonConvert.DeserializeObject<XwaModVersions>(contentJson);
        }
        catch (Exception ex)
        {
            return "is not valid JSON: " + ex.Message;
        }

        if (pack is null)
        {
            return "is not valid JSON";
        }

        if (pack.Mods.Count == 0)
        {
            return "contains no mods";
        }

        foreach (XwaMod mod in pack.Mods)
        {
            if (modVersions.Mods.Find(t => string.Equals(t.ModId, mod.ModId, StringComparison.OrdinalIgnoreCase)) is null)
            {
                modVersions.Mods.Add(mod);
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the mod list from cached JSON only, without any network access.
    /// Returns null when nothing is cached yet.
    /// </summary>
    public static XwaModVersions? LoadFromCache()
    {
        XwaModVersions? xwaModVersions = LoadCachedDocument(GlobalSettings.XwaInstallerPackagesUrl);

        if (xwaModVersions is null)
        {
            return null;
        }

        foreach (string modPack in xwaModVersions.ModPacks)
        {
            MergeCachedDocument(xwaModVersions, modPack);
        }

        foreach (string modPack in GlobalSettings.AdditionalModPackUrls)
        {
            MergeCachedDocument(xwaModVersions, modPack);
        }

        foreach (string modPack in ManagerSettings.Default.AdditionalModSources)
        {
            MergeCachedDocument(xwaModVersions, modPack);
        }

        return xwaModVersions;
    }

    private static void MergeCachedDocument(XwaModVersions modVersions, string modPackUrl)
    {
        XwaModVersions? pack = LoadCachedDocument(modPackUrl);

        if (pack is null)
        {
            return;
        }

        foreach (XwaMod mod in pack.Mods)
        {
            if (modVersions.Mods.Find(t => string.Equals(t.ModId, mod.ModId, StringComparison.OrdinalIgnoreCase)) is null)
            {
                modVersions.Mods.Add(mod);
            }
        }
    }

    private static XwaModVersions? LoadCachedDocument(string contentUrl)
    {
        try
        {
            string cachePath = GetCachePath(contentUrl);

            if (!File.Exists(cachePath))
            {
                return null;
            }

            string json = File.ReadAllText(cachePath, GlobalSettings.Encoding);

            return string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<XwaModVersions>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cache file path for a URL. The name must be unique per URL: different
    /// hosts commonly serve the same file name (e.g. "app-versions.json"), and
    /// sharing one cache file would make them overwrite each other.
    /// </summary>
    private static string GetCachePath(string contentUrl)
    {
        string fileName = Path.GetFileName(new Uri(contentUrl).AbsolutePath);

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "mod-versions.json";
        }

        return Path.Combine(GlobalSettings.InstallerPackagesFolder, GetCacheKey(contentUrl) + "_" + fileName);
    }

    private static async Task<string?> GetContentJsonAsync(string? contentUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(contentUrl))
        {
            return null;
        }

        // Cache the body plus its ETag/Last-Modified so an unchanged document is
        // served from disk (304 Not Modified) instead of being re-downloaded.
        string cachePath = GetCachePath(contentUrl);
        string metaPath = cachePath + ".meta";

        string? text = await WebClientHelpers
            .DownloadTextCachedAsync(contentUrl, cachePath, metaPath, cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Builds a short, filesystem-safe key that is unique per URL, so two hosts
    /// serving the same file name do not share a cache entry.
    /// </summary>
    private static string GetCacheKey(string contentUrl)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(contentUrl));

        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }
}
