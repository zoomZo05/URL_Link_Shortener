namespace UrlShortener.Api;

public sealed class ShortLinkOptions
{
    public const string SectionName = "ShortLink";
    public string BaseUrl { get; init; } = "http://localhost:5000";
    public int CodeLength { get; init; } = 8;
    public string[] AllowedOrigins { get; init; } = ["http://localhost:5173"];
}
