using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Abstractions;

public interface IUrlRepository
{
    IReadOnlyList<ShortLinkSnapshot> GetAll();
    ShortLinkSnapshot? GetByCode(string code);
    bool TryAdd(ShortLink link);
    bool UpdateStatus(string code, bool isActive);
    bool SoftDelete(string code);
    LinkAccessStatus ResolveAndRecordAccess(string code, DateTime accessedAtUtc, out ShortLinkSnapshot? link);
}
