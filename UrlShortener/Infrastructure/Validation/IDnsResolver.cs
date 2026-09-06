using System.Net;

namespace UrlShortener.Infrastructure.Validation;

public interface IDnsResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken ct);
}
