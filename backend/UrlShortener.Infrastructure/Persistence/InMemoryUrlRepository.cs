using System.Collections.Concurrent;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public sealed class InMemoryUrlRepository : IUrlRepository
{
    private readonly ConcurrentDictionary<string, ShortLink> links = new(StringComparer.Ordinal);

    public IReadOnlyList<ShortLinkSnapshot> GetAll()
    {
        return links.Values.Select(link => link.GetSnapshot()).ToArray();
    }

    public ShortLinkSnapshot? GetByCode(string code)
    {
        if (!links.TryGetValue(code, out var link))
        {
            return null;
        }

        return link.GetSnapshot();
    }

    public bool TryAdd(ShortLink link)
    {
        return links.TryAdd(link.ShortCode, link);
    }

    public bool UpdateStatus(string code, bool isActive)
    {
        if (!links.TryGetValue(code, out var link))
        {
            return false;
        }

        return link.SetActive(isActive);
    }

    public bool SoftDelete(string code)
    {
        if (!links.TryGetValue(code, out var link))
        {
            return false;
        }

        link.SoftDelete();

        return true;
    }

    public LinkAccessStatus ResolveAndRecordAccess(string code, DateTime accessedAtUtc, out ShortLinkSnapshot? snapshot)
    {
        if (!links.TryGetValue(code, out var link))
        {
            snapshot = null;
            return LinkAccessStatus.NotFound;
        }

        var status = link.ResolveAccess(accessedAtUtc, out _);
        snapshot = status == LinkAccessStatus.Active ? link.GetSnapshot() : null;
        return status;
    }
}
