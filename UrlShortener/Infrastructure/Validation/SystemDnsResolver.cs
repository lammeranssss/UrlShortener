using System.Net;

namespace UrlShortener.Infrastructure.Validation;

public sealed class SystemDnsResolver : IDnsResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken ct)
    {
        return Dns.GetHostAddressesAsync(host, ct);
    }
}
