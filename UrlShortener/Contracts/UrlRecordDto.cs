namespace UrlShortener.Contracts;

public sealed record UrlRecordDto(
    long Id,
    string OriginalUrl,
    string ShortCode,
    long ClickCount,
    DateTime CreatedAt,
    DateTime? LastAccessedAt);
    