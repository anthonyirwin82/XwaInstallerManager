using System.Net.Http;
using XwaInstallerLibCore.Platform;

namespace XwaInstallerManager;

public static class HttpDownloadHelpers
{
    private const int BufferSize = 81920;

    public static async Task DownloadFileAsync(string uri, string dst, IProgress<HttpDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        var handler = new DnsHandler();

        using var client = GlobalSettings.CreateHttpClient(handler);

        HttpResponseMessage response;

        try
        {
            response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            handler.EndPoint = GlobalSettings.AlternateEndPoint;

            try
            {
                response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
        }

        try
        {
            long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
            using Stream download = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            if (File.Exists(dst))
            {
                File.Delete(dst);
            }

            using FileStream destination = File.OpenWrite(dst);

            if (progress is null || contentLength == 0L)
            {
                await download.CopyToAsync(destination, BufferSize, cancellationToken).ConfigureAwait(false);
                return;
            }

            byte[] buffer = new byte[BufferSize];
            long totalBytesRead = 0L;

            while (true)
            {
                int read = await download.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                totalBytesRead += read;

                progress?.Report(new HttpDownloadProgress
                {
                    TotalLength = contentLength,
                    ReadLength = totalBytesRead,
                    Progress = Convert.ToInt32(100f * (float)totalBytesRead / (float)contentLength)
                });
            }

            progress?.Report(new HttpDownloadProgress
            {
                TotalLength = contentLength,
                ReadLength = contentLength,
                Progress = 100
            });
        }
        finally
        {
            response?.Dispose();
        }
    }

    public static async Task DownloadFileAsync(string uri, string dst, Action<string> progressMessage, CancellationToken cancellationToken = default)
    {
        var progress = new Progress<HttpDownloadProgress>(f =>
        {
            if (f.ReadLength == f.TotalLength)
            {
                progressMessage(PlatformInfo.FormatByteSize(f.TotalLength));
            }
            else
            {
                progressMessage($"{f.Progress}% {PlatformInfo.FormatByteSize(f.ReadLength)}/{PlatformInfo.FormatByteSize(f.TotalLength)}");
            }
        });

        await DownloadFileAsync(uri, dst, progress, cancellationToken);
    }

    public static long RetrieveFileLength(string uri)
    {
        using var dnsHandler = new DnsHandler();
        using var httpClient = GlobalSettings.CreateHttpClient(dnsHandler);
        HttpResponseMessage? httpResponseMessage = null;

        try
        {
            try
            {
                httpResponseMessage = httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            }
            catch
            {
                dnsHandler.EndPoint = GlobalSettings.AlternateEndPoint;

                try
                {
                    httpResponseMessage = httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                }
                catch
                {
                    return 0L;
                }
            }

            return httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault();
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }

    public static DateTime? RetrieveFileDate(string uri)
    {
        using var dnsHandler = new DnsHandler();
        using var httpClient = GlobalSettings.CreateHttpClient(dnsHandler);
        HttpResponseMessage? httpResponseMessage = null;

        try
        {
            try
            {
                httpResponseMessage = httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            }
            catch
            {
                dnsHandler.EndPoint = GlobalSettings.AlternateEndPoint;

                try
                {
                    httpResponseMessage = httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                }
                catch
                {
                    return null;
                }
            }

            return httpResponseMessage.Content.Headers.LastModified?.DateTime;
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }
}
