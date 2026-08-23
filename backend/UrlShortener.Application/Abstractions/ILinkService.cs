using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Abstractions;

public interface ILinkService
{
    IReadOnlyList<ShortLinkSnapshot> List();
    CreateLinkResult Create(string originalUrl, string? customAlias, PlatformOverrides? platformOverrides = null);
    ShortLinkSnapshot? Get(string code);
    RedirectResolution ResolveAndRecordAccess(string code, string? userAgent = null);
    bool UpdateStatus(string code, bool isActive);
    bool Delete(string code);
}

public sealed record RedirectResolution(
    LinkAccessStatus Status,
    string? OriginalUrl);
