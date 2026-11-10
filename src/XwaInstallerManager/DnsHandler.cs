using System.Net;
using System.Net.Http;
using DnsClient;

namespace XwaInstallerManager;

/// <summary>
/// Optionally resolves a request's host through an alternate DNS server and
/// rewrites the URI to the resolved IP (preserving the Host header).
/// </summary>
public class DnsHandler : HttpClientHandler
{
    public IPEndPoint? EndPoint { get; set; }

    public DnsHandler()
    {
    }

    public DnsHandler(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (EndPoint is not null && request.RequestUri is not null)
        {
            try
            {
                string host = request.RequestUri.Host;
                IPAddress[] addresses = DnsQueryExtensions.GetHostEntry(new LookupClient(new[] { EndPoint }), host).AddressList;

                if (addresses.Length > 0)
                {
                    var uriBuilder = new UriBuilder(request.RequestUri)
                    {
                        Host = addresses[0].ToString()
                    };

                    request.RequestUri = uriBuilder.Uri;
                    request.Headers.Host = host;
                }
            }
            catch
            {
                // Fall back to the system resolver.
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
