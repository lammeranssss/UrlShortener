using System.Net;
using System.Net.Sockets;

namespace UrlShortener.Infrastructure.Validation;

public sealed class UrlValidator
{
    private readonly IDnsResolver _dnsResolver;

    public UrlValidator(IDnsResolver dnsResolver)
    {
        _dnsResolver = dnsResolver;
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(
        string rawUrl, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl.Length > 2048)
            return (false, "URL пуст или превышает лимит в 2048 символов.");

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) || 
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return (false, "Поддерживаются только протоколы HTTP и HTTPS.");
        }

        try
        {
            // Отмена CancellationToken пробрасывается наверх по стеку, 
            // не маскируясь под бизнес-ошибку валидации.
            IPAddress[] addresses = await _dnsResolver.GetHostAddressesAsync(uri.DnsSafeHost, ct);
            if (addresses.Length == 0)
                return (false, "Не удалось получить IP-адреса для указанного домена.");

            foreach (var address in addresses)
            {
                if (IsPrivateOrLoopback(address))
                    return (false, "Указанный URL ведет во внутреннюю или локальную сеть (SSRF Protection).");
            }
        }
        catch (SocketException)
        {
            return (false, "Не удалось разрешить имя хоста в DNS.");
        }

        return (true, null);
    }

    private static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Проверка диапазонов RFC 4193 (Unique Local) и RFC 4291 (Link/Site-Local, Multicast)
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal || ip.IsIPv6Multicast)
                return true;

            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }
            else
            {
                byte[] v6Bytes = ip.GetAddressBytes();

                // Global Unicast (RFC 4291) должен принадлежать диапазону 2000::/3.
                // Старшие 3 бита первого байта обязаны быть равны 001 (0x20).
                bool isGlobalUnicast = (v6Bytes[0] & 0xE0) == 0x20;
                if (!isGlobalUnicast)
                    return true;

                // Documentation Range 2001:db8::/32 (RFC 3849)
                if (v6Bytes[0] == 0x20 && v6Bytes[1] == 0x01 && v6Bytes[2] == 0x0d && v6Bytes[3] == 0xb8)
                    return true;

                // Discard-Only Prefix 100::/64 (RFC 6666)
                if (v6Bytes[0] == 0x01 && v6Bytes[1] == 0x00 && v6Bytes[2] == 0x00 && v6Bytes[3] == 0x00)
                    return true;

                return false;
            }
        }

        if (ip.AddressFamily != AddressFamily.InterNetwork) 
            return true; // Fail-Safe для нетипичных семейств сокетов

        byte[] bytes = ip.GetAddressBytes();

        // Фильтрация RFC 1918 (Private IPv4), Loopback (127.0.0.0/8) и APIPA/Metadata (169.254.x.x)
        return bytes[0] switch
        {
            10 => true,                              // 10.0.0.0/8
            127 => true,                             // 127.0.0.0/8
            172 => bytes[1] >= 16 && bytes[1] <= 31, // 172.16.0.0/12
            192 => bytes[1] == 168,                  // 192.168.0.0/16
            169 => bytes[1] == 254,                  // 169.254.x.x (AWS Metadata API / APIPA)
            0 => true,                               // 0.0.0.0/8 (Current network)
            _ => false
        };
    }
}