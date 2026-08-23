using System.Text.RegularExpressions;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Links;

public sealed class LinkService(
    IUrlRepository repository,
    IShortCodeGenerator codeGenerator,
    IPlatformDetector? detector = null,
    IRegistrableDomainPolicy? domainPolicy = null) : ILinkService
{
    private readonly IPlatformDetector detector = detector ?? new UserAgentPlatformDetector();
    private readonly IRegistrableDomainPolicy? domainPolicy = domainPolicy;
    private static readonly Regex AliasPattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> ReservedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "swagger", "health", "stats"
    };

    public CreateLinkResult Create(string originalUrl, string? customAlias, PlatformOverrides? platformOverrides = null)
    {
        if (!Uri.TryCreate(originalUrl?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new InvalidLinkInput("OriginalUrl must be an absolute http or https URL.");
        }

        var code = customAlias?.Trim();
        if (code is not null && (code.Length == 0 || !AliasPattern.IsMatch(code)))
        {
            return new InvalidLinkInput("CustomAlias may contain only letters, numbers, hyphens, and underscores.");
        }

        if (code is not null && ReservedCodes.Contains(code))
        {
            return new InvalidLinkInput("CustomAlias is reserved by the system.");
        }

        var overrideValidation = ValidatePlatformOverrides(uri, platformOverrides);
        if (overrideValidation is not null)
        {
            return new InvalidLinkInput(overrideValidation);
        }

        if (code is null)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                code = codeGenerator.Generate();
                var generatedLink = new ShortLink
                {
                    ShortCode = code,
                    OriginalUrl = uri.ToString(),
                    PlatformOverrides = platformOverrides ?? new()
                };
                if (repository.TryAdd(generatedLink))
                {
                    return new LinkCreated(generatedLink);
                }
            }

            return new ShortCodeGenerationUnavailable();
        }

        var link = new ShortLink
        {
            ShortCode = code,
            OriginalUrl = uri.ToString(),
            PlatformOverrides = platformOverrides ?? new()
        };
        if (repository.TryAdd(link))
        {
            return new LinkCreated(link);
        }

        return new CustomAliasAlreadyInUse(code);
    }

    public ShortLinkSnapshot? Get(string code)
    {
        return repository.GetByCode(code);
    }

    public IReadOnlyList<ShortLinkSnapshot> List()
    {
        return repository.GetAll()
            .Where(link => !link.IsDeleted)
            .OrderByDescending(link => link.CreatedAtUtc)
            .ToArray();
    }

    public RedirectResolution ResolveAndRecordAccess(string code, string? userAgent = null)
    {
        var status = repository.ResolveAndRecordAccess(code, DateTime.UtcNow, out var link);
        if (status != LinkAccessStatus.Active || link is null)
        {
            return new RedirectResolution(status, null);
        }

        var candidate = detector.Detect(userAgent) switch
        {
            LinkPlatform.Ios => link.PlatformOverrides.IosUrl,
            LinkPlatform.Android => link.PlatformOverrides.AndroidUrl,
            _ => null
        };

        var destination = candidate is not null && SameRegistrableDomain(link.OriginalUrl, candidate)
            ? candidate
            : link.OriginalUrl;

        return new RedirectResolution(status, destination);
    }

    public bool UpdateStatus(string code, bool isActive)
    {
        return repository.UpdateStatus(code, isActive);
    }

    public bool Delete(string code)
    {
        return repository.SoftDelete(code);
    }

    private string? ValidatePlatformOverrides(Uri originalUri, PlatformOverrides? overrides)
    {
        if (overrides is null)
        {
            return null;
        }

        foreach (var value in new[] { overrides.IosUrl, overrides.AndroidUrl })
        {
            if (value is null)
            {
                continue;
            }

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return "Platform override URLs must be absolute http or https URLs.";
            }

            if (!IsSameRegistrableDomain(originalUri.Host, uri.Host))
            {
                return "Platform override URLs must use the same registrable domain as the default destination.";
            }
        }

        return null;
    }

    private bool SameRegistrableDomain(string originalUrl, string candidateUrl)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var original) ||
            !Uri.TryCreate(candidateUrl, UriKind.Absolute, out var candidate))
        {
            return false;
        }

        return IsSameRegistrableDomain(original.Host, candidate.Host);
    }

    private bool IsSameRegistrableDomain(string originalHost, string candidateHost)
    {
        return domainPolicy?.IsSame(originalHost, candidateHost) ?? SameRegistrableHosts(originalHost, candidateHost);
    }

    private static bool SameRegistrableHosts(string originalHost, string candidateHost)
    {
        return string.Equals(
            RegistrableDomain(originalHost),
            RegistrableDomain(candidateHost),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string RegistrableDomain(string host)
    {
        var labels = host.TrimEnd('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length <= 2 || System.Net.IPAddress.TryParse(host, out _))
        {
            return host;
        }

        var suffix = $"{labels[^2]}.{labels[^1]}";
        var multiLabelSuffix = suffix is "co.th" or "co.uk" or "com.au" or "co.jp" or "co.nz" or "com.br";
        var start = multiLabelSuffix ? labels.Length - 3 : labels.Length - 2;
        return string.Join('.', labels[start..]);
    }
}
