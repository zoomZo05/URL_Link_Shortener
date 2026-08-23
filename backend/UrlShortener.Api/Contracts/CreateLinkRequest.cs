namespace UrlShortener.Api.Contracts;

public sealed class CreateLinkRequest
{
    public string? OriginalUrl { get; init; }
    public string? CustomAlias { get; init; }
    public PlatformOverridesRequest? PlatformOverrides { get; init; }
}

public sealed class PlatformOverridesRequest
{
    public string? Ios { get; init; }
    public string? Android { get; init; }
}
