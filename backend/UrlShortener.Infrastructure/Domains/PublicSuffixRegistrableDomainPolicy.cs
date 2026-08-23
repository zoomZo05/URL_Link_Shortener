using Nager.PublicSuffix;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure.Domains;

public sealed class PublicSuffixRegistrableDomainPolicy : IRegistrableDomainPolicy
{
    private readonly IDomainParser parser;

    public PublicSuffixRegistrableDomainPolicy(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<SimpleHttpRuleProvider> logger)
    {
        // Nager loads the official Public Suffix List, including multi-part suffixes such as co.th.
        // The list is an external resource; keep this adapter isolated so it can later use a local snapshot.
        var provider = new SimpleHttpRuleProvider(
            configuration,
            httpClient,
            logger,
            TldRuleDivisionFilter.All);
        provider.BuildAsync().GetAwaiter().GetResult();
        parser = new DomainParser(provider);
    }

    public bool IsSame(string firstHost, string secondHost)
    {
        if (System.Net.IPAddress.TryParse(firstHost, out _) ||
            System.Net.IPAddress.TryParse(secondHost, out _))
        {
            return string.Equals(firstHost, secondHost, StringComparison.OrdinalIgnoreCase);
        }

        var first = parser.Parse(firstHost)?.RegistrableDomain;
        var second = parser.Parse(secondHost)?.RegistrableDomain;
        if (first is null || second is null)
        {
            return string.Equals(firstHost, secondHost, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
