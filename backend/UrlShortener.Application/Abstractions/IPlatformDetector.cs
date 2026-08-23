namespace UrlShortener.Application.Abstractions;

public enum LinkPlatform
{
    Default,
    Ios,
    Android
}

public interface IPlatformDetector
{
    LinkPlatform Detect(string? userAgent);
}

public sealed class UserAgentPlatformDetector : IPlatformDetector
{
    public LinkPlatform Detect(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return LinkPlatform.Default;
        }

        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return LinkPlatform.Android;
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
        {
            return LinkPlatform.Ios;
        }

        return LinkPlatform.Default;
    }
}
