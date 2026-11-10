using System.Diagnostics;
using System.Net.Http;
using XwaInstallerLibCore;

namespace XwaInstallerManager;

internal static class WebClientHelpers
{
    public static string[] DownloadLines(string directoryUrl, string fileName)
    {
        return DownloadLines(new Uri(Path.Combine(new Uri(directoryUrl).AbsoluteUri, fileName)).AbsoluteUri);
    }

    public static string[] DownloadLines(string contentUrl)
    {
        string? text = DownloadText(contentUrl);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text.GetLines(true);
    }

    public static string? DownloadText(string contentUrl)
    {
        if (string.IsNullOrEmpty(contentUrl))
        {
            return null;
        }

        string? result = null;
        using var dnsHandler = new DnsHandler();
        using var httpClient = GlobalSettings.CreateHttpClient(dnsHandler);

        try
        {
            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    if (i >= 4)
                    {
                        dnsHandler.EndPoint = GlobalSettings.AlternateEndPoint;
                    }

                    result = httpClient.GetStringAsync(contentUrl).GetAwaiter().GetResult();
                }
                catch
                {
                    if (i == 6)
                    {
                        throw;
                    }

                    Thread.Sleep(100);
                    continue;
                }

                break;
            }
        }
        catch (Exception ex)
        {
            result = null;
            MessageService.ShowError(ex.ToString(), "Press Ctrl+C to copy this error message");
        }

        return result;
    }

    /// <summary>
    /// Downloads text using a conditional request (ETag / Last-Modified) so an
    /// unchanged document is not transferred again. Returns the cached text when
    /// the server answers 304 Not Modified.
    /// </summary>
    /// <param name="contentUrl">The URL to fetch.</param>
    /// <param name="cachePath">Where the last successful body is stored.</param>
    /// <param name="metaPath">Where the ETag / Last-Modified values are stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The text, or null when it could not be retrieved and no cache exists.</returns>
    public static async Task<string?> DownloadTextCachedAsync(
        string contentUrl,
        string cachePath,
        string metaPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentUrl))
        {
            return null;
        }

        string? cachedText = TryReadCache(cachePath);
        CacheMetadata metadata = CacheMetadata.Load(metaPath);

        using var dnsHandler = new DnsHandler();
        using var httpClient = GlobalSettings.CreateHttpClient(dnsHandler);

        for (int i = 1; i <= 6; i++)
        {
            try
            {
                if (i >= 4)
                {
                    dnsHandler.EndPoint = GlobalSettings.AlternateEndPoint;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, contentUrl);

                // Only send validators when we actually hold a cached body.
                if (cachedText is not null)
                {
                    if (!string.IsNullOrEmpty(metadata.ETag) &&
                        System.Net.Http.Headers.EntityTagHeaderValue.TryParse(metadata.ETag, out var etag))
                    {
                        request.Headers.IfNoneMatch.Add(etag);
                    }

                    if (DateTimeOffset.TryParse(metadata.LastModified, out var lastModified))
                    {
                        request.Headers.IfModifiedSince = lastModified;
                    }
                }

                using HttpResponseMessage response = await httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotModified && cachedText is not null)
                {
                    // Unchanged: reuse the cached body, no re-download.
                    return cachedText;
                }

                response.EnsureSuccessStatusCode();

                string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(text))
                {
                    return cachedText;
                }

                // Only rewrite the cache when the content actually changed.
                if (!string.Equals(text, cachedText, StringComparison.Ordinal))
                {
                    TryWriteCache(cachePath, text);
                }

                var newMetadata = new CacheMetadata
                {
                    ETag = response.Headers.ETag?.ToString() ?? string.Empty,
                    LastModified = response.Content.Headers.LastModified?.ToString("R") ?? string.Empty
                };

                if (newMetadata.ETag != metadata.ETag || newMetadata.LastModified != metadata.LastModified)
                {
                    TryWriteCache(metaPath, newMetadata.Serialize());
                }

                return text;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (i == 6)
                {
                    // Network failed: fall back to whatever we have cached.
                    return cachedText;
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        return cachedText;
    }

    private static string? TryReadCache(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path, GlobalSettings.Encoding) : null;
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteCache(string path, string contents)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents, GlobalSettings.Encoding);
        }
        catch
        {
            // Caching is best effort; never fail the caller over it.
        }
    }

    private sealed class CacheMetadata
    {
        public string ETag { get; set; } = string.Empty;

        public string LastModified { get; set; } = string.Empty;

        public static CacheMetadata Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new CacheMetadata();
                }

                string[] lines = File.ReadAllLines(path, GlobalSettings.Encoding);

                return new CacheMetadata
                {
                    ETag = lines.Length > 0 ? lines[0] : string.Empty,
                    LastModified = lines.Length > 1 ? lines[1] : string.Empty
                };
            }
            catch
            {
                return new CacheMetadata();
            }
        }

        public string Serialize() => ETag + Environment.NewLine + LastModified;
    }
}
