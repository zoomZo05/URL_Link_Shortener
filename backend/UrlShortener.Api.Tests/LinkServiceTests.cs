using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Links;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Api.Tests;

public sealed class LinkServiceTests
{
    [Fact]
    public void Create_generates_a_code_and_stores_the_original_url()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("AbC123xy"));

        var result = service.Create("https://google.com", null);

        var created = Assert.IsType<LinkCreated>(result);
        Assert.Equal("AbC123xy", created.Link.ShortCode);
        Assert.Equal("https://google.com/", created.Link.OriginalUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    public void Create_rejects_invalid_urls(string originalUrl)
    {
        var service = new LinkService(new InMemoryUrlRepository(), new FixedCodeGenerator("abc12345"));

        var result = service.Create(originalUrl, null);

        Assert.IsType<InvalidLinkInput>(result);
    }

    [Fact]
    public void Resolve_access_increments_click_count()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("abc12345"));
        service.Create("https://google.com", null);

        var resolution = service.ResolveAndRecordAccess("abc12345");

        Assert.Equal(LinkAccessStatus.Active, resolution.Status);
        Assert.Equal(1, repository.GetByCode("abc12345")!.ClickCount);
        Assert.NotNull(repository.GetByCode("abc12345")!.LastAccessedAtUtc);
    }

    [Fact]
    public void Custom_aliases_are_case_sensitive_and_collisions_are_rejected()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("generated"));

        Assert.IsType<LinkCreated>(service.Create("https://google.com", "Promo"));
        Assert.IsType<LinkCreated>(service.Create("https://google.com", "promo"));
        Assert.IsType<CustomAliasAlreadyInUse>(service.Create("https://google.com", "Promo"));
    }

    [Fact]
    public void Reserved_aliases_are_rejected_without_case_sensitive_route_collisions()
    {
        var service = new LinkService(new InMemoryUrlRepository(), new FixedCodeGenerator("generated"));

        var result = service.Create("https://google.com", "API");

        var invalid = Assert.IsType<InvalidLinkInput>(result);
        Assert.Contains("reserved", invalid.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generated_code_collision_is_retried_atomically()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new SequenceCodeGenerator("same", "same", "next"));
        Assert.IsType<LinkCreated>(service.Create("https://google.com", null));

        var result = service.Create("https://example.com", null);

        var created = Assert.IsType<LinkCreated>(result);
        Assert.Equal("next", created.Link.ShortCode);
    }

    [Fact]
    public void Concurrent_accesses_do_not_lose_clicks()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("abc12345"));
        service.Create("https://google.com", null);

        Parallel.For(0, 100, _ => service.ResolveAndRecordAccess("abc12345"));

        Assert.Equal(100, repository.GetByCode("abc12345")!.ClickCount);
    }

    [Fact]
    public void Ios_user_agent_uses_ios_override_on_the_same_registrable_domain()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("ios12345"));
        service.Create(
            "https://www.gulf.co.th",
            null,
            new PlatformOverrides("https://download.gulf.co.th/iphone.ipa", "https://download.gulf.co.th/android.apk"));

        var resolution = service.ResolveAndRecordAccess(
            "ios12345",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X)");

        Assert.Equal(LinkAccessStatus.Active, resolution.Status);
        Assert.Equal("https://download.gulf.co.th/iphone.ipa", resolution.OriginalUrl);
    }

    [Fact]
    public void Android_user_agent_uses_android_override()
    {
        var service = new LinkService(new InMemoryUrlRepository(), new FixedCodeGenerator("and12345"));
        service.Create(
            "https://www.gulf.co.th",
            null,
            new PlatformOverrides(null, "https://download.gulf.co.th/android.apk"));

        var resolution = service.ResolveAndRecordAccess(
            "and12345",
            "Mozilla/5.0 (Linux; Android 13; Pixel 7)");

        Assert.Equal("https://download.gulf.co.th/android.apk", resolution.OriginalUrl);
    }

    [Fact]
    public void Cross_domain_platform_override_is_rejected_during_creation()
    {
        var service = new LinkService(new InMemoryUrlRepository(), new FixedCodeGenerator("safe12345"));
        var result = service.Create(
            "https://www.gulf.co.th",
            null,
            new PlatformOverrides("https://example.com/iphone.ipa", null));

        var invalid = Assert.IsType<InvalidLinkInput>(result);
        Assert.Contains("registrable domain", invalid.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cross_domain_platform_override_falls_back_to_original_at_redirect_time()
    {
        var repository = new InMemoryUrlRepository();
        repository.TryAdd(new ShortLink
        {
            ShortCode = "unsafe123",
            OriginalUrl = "https://www.gulf.co.th",
            PlatformOverrides = new PlatformOverrides("https://example.com/iphone.ipa", null)
        });
        var service = new LinkService(repository, new FixedCodeGenerator("unused"));

        var resolution = service.ResolveAndRecordAccess(
            "unsafe123",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0)");

        Assert.Equal("https://www.gulf.co.th", resolution.OriginalUrl);
    }

    [Fact]
    public void Default_user_agent_uses_original_url_and_counts_the_click()
    {
        var repository = new InMemoryUrlRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("def12345"));
        service.Create(
            "https://www.gulf.co.th",
            null,
            new PlatformOverrides("https://download.gulf.co.th/iphone.ipa", "https://download.gulf.co.th/android.apk"));

        var resolution = service.ResolveAndRecordAccess(
            "def12345",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        Assert.Equal("https://www.gulf.co.th/", resolution.OriginalUrl);
        Assert.Equal(1, repository.GetByCode("def12345")!.ClickCount);
    }

    [Fact]
    public void Invalid_platform_override_scheme_is_rejected()
    {
        var service = new LinkService(new InMemoryUrlRepository(), new FixedCodeGenerator("abc12345"));

        var result = service.Create(
            "https://www.gulf.co.th",
            null,
            new PlatformOverrides("ftp://download.gulf.co.th/iphone.ipa", null));

        Assert.IsType<InvalidLinkInput>(result);
    }

    private sealed class FixedCodeGenerator(string code) : IShortCodeGenerator
    {
        public string Generate()
        {
            return code;
        }
    }

    private sealed class SequenceCodeGenerator(params string[] codes) : IShortCodeGenerator
    {
        private int index;

        public string Generate()
        {
            return codes[Math.Min(index++, codes.Length - 1)];
        }
    }
}
