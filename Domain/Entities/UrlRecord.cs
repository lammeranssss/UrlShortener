namespace UrlShortener.Domain.Entities;

public sealed class UrlRecord
{
    public long Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long ClickCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}