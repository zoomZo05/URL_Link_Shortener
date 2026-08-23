namespace UrlShortener.Domain.Entities;

public sealed class ShortLink
{
    private readonly object stateLock = new();

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ShortCode { get; init; }
    public required string OriginalUrl { get; init; }
    public PlatformOverrides PlatformOverrides { get; init; } = new();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? LastAccessedAtUtc { get; private set; }
    public long ClickCount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; }

    public LinkAccessStatus ResolveAccess(DateTime accessedAtUtc, out string? originalUrl)
    {
        lock (stateLock)
        {
            if (IsDeleted)
            {
                originalUrl = null;
                return LinkAccessStatus.NotFound;
            }

            if (!IsActive)
            {
                originalUrl = null;
                return LinkAccessStatus.Disabled;
            }

            ClickCount++;
            LastAccessedAtUtc = accessedAtUtc;
            originalUrl = OriginalUrl;
            return LinkAccessStatus.Active;
        }
    }

    public bool SetActive(bool isActive)
    {
        lock (stateLock)
        {
            if (IsDeleted)
            {
                return false;
            }

            IsActive = isActive;
            return true;
        }
    }

    public void SoftDelete()
    {
        lock (stateLock)
        {
            IsDeleted = true;
        }
    }

    public ShortLinkSnapshot GetSnapshot()
    {
        lock (stateLock)
        {
            return new ShortLinkSnapshot(
                Id,
                ShortCode,
                OriginalUrl,
                PlatformOverrides,
                CreatedAtUtc,
                LastAccessedAtUtc,
                ClickCount,
                IsActive,
                IsDeleted);
        }
    }
}

public enum LinkAccessStatus
{
    NotFound,
    Disabled,
    Active
}

public sealed record ShortLinkSnapshot(
    Guid Id,
    string ShortCode,
    string OriginalUrl,
    PlatformOverrides PlatformOverrides,
    DateTime CreatedAtUtc,
    DateTime? LastAccessedAtUtc,
    long ClickCount,
    bool IsActive,
    bool IsDeleted);

public sealed record PlatformOverrides(
    string? IosUrl = null,
    string? AndroidUrl = null);
