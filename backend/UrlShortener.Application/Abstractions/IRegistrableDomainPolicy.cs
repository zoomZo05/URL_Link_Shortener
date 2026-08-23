namespace UrlShortener.Application.Abstractions;

public interface IRegistrableDomainPolicy
{
    bool IsSame(string firstHost, string secondHost);
}
