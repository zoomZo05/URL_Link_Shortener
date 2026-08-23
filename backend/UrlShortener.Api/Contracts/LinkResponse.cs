using UrlShortener.Domain.Entities;

namespace UrlShortener.Api.Contracts;

public sealed record LinkResponse(
    Guid Id,
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    PlatformOverrides PlatformOverrides,
    long ClickCount,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastAccessedAtUtc);
