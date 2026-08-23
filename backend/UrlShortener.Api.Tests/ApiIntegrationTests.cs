using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Abstractions;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<ApiIntegrationTests.ApiFactory>
{
    private readonly ApiFactory factory;

    public ApiIntegrationTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_returns_created_link_with_location_and_json_contract()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/links", new
        {
            originalUrl = "https://example.com/main",
            customAlias = "create-contract",
            platformOverrides = new
            {
                ios = "https://download.example.com/ios",
                android = "https://download.example.com/android"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/links/create-contract/stats", response.Headers.Location?.OriginalString);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("create-contract", body.RootElement.GetProperty("shortCode").GetString());
        Assert.Equal("https://gul.fy:5001/create-contract", body.RootElement.GetProperty("shortUrl").GetString());
        Assert.Equal("https://example.com/main", body.RootElement.GetProperty("originalUrl").GetString());
        Assert.Equal("https://download.example.com/ios", body.RootElement.GetProperty("platformOverrides").GetProperty("iosUrl").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("clickCount").GetInt64());
        Assert.True(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("lastAccessedAtUtc").ValueKind);
    }

    [Fact]
    public async Task Create_maps_invalid_and_conflicting_requests_to_documented_errors()
    {
        using var client = factory.CreateClient();

        var invalidResponse = await client.PostAsJsonAsync("/api/links", new { originalUrl = "not-a-url" });
        var firstAliasResponse = await client.PostAsJsonAsync("/api/links", new { originalUrl = "https://example.com", customAlias = "duplicate-alias" });
        var conflictResponse = await client.PostAsJsonAsync("/api/links", new { originalUrl = "https://example.com", customAlias = "duplicate-alias" });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("Invalid link", await GetProblemTitle(invalidResponse));
        Assert.Equal(HttpStatusCode.Created, firstAliasResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("Short code conflict", await GetProblemTitle(conflictResponse));
    }

    [Fact]
    public async Task List_and_stats_expose_created_link_and_redirect_analytics()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await CreateLink(client, "analytics-link");

        var listResponse = await client.GetAsync("/api/links");
        var redirectResponse = await client.GetAsync("/analytics-link");
        var statsResponse = await client.GetAsync("/api/links/analytics-link/stats");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using (var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()))
        {
            Assert.Contains(list.RootElement.EnumerateArray(), link => link.GetProperty("shortCode").GetString() == "analytics-link");
        }

        Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
        Assert.Equal("https://example.com/", redirectResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        using var stats = JsonDocument.Parse(await statsResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, stats.RootElement.GetProperty("clickCount").GetInt64());
        Assert.Equal(JsonValueKind.String, stats.RootElement.GetProperty("lastAccessedAtUtc").ValueKind);
    }

    [Fact]
    public async Task Redirect_uses_platform_override_from_user_agent()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var createResponse = await client.PostAsJsonAsync("/api/links", new
        {
            originalUrl = "https://www.example.com",
            customAlias = "ios-link",
            platformOverrides = new { ios = "https://download.example.com/app", android = (string?)null }
        });
        createResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (iPhone; CPU iPhone OS 16_0)");

        var response = await client.GetAsync("/ios-link");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://download.example.com/app", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Status_and_delete_follow_the_link_lifecycle_contract()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await CreateLink(client, "lifecycle-link");

        var disableResponse = await client.PatchAsJsonAsync("/api/links/lifecycle-link/status", new { isActive = false });
        var disabledRedirect = await client.GetAsync("/lifecycle-link");
        var reenableResponse = await client.PatchAsJsonAsync("/api/links/lifecycle-link/status", new { isActive = true });
        var activeRedirect = await client.GetAsync("/lifecycle-link");
        var deleteResponse = await client.DeleteAsync("/api/links/lifecycle-link");
        var statsAfterDelete = await client.GetAsync("/api/links/lifecycle-link/stats");
        var redirectAfterDelete = await client.GetAsync("/lifecycle-link");

        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, disabledRedirect.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, reenableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Found, activeRedirect.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, statsAfterDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, redirectAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Unknown_resources_return_not_found()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var statsResponse = await client.GetAsync("/api/links/missing/stats");
        var statusResponse = await client.PatchAsJsonAsync("/api/links/missing/status", new { isActive = false });
        var deleteResponse = await client.DeleteAsync("/api/links/missing");
        var redirectResponse = await client.GetAsync("/missing");

        Assert.Equal(HttpStatusCode.NotFound, statsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, redirectResponse.StatusCode);
    }

    private static async Task CreateLink(HttpClient client, string alias)
    {
        var response = await client.PostAsJsonAsync("/api/links", new { originalUrl = "https://example.com", customAlias = alias });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<string?> GetProblemTitle(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("title").GetString();
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                ReplaceService<IUrlRepository>(services, new InMemoryUrlRepository());
                ReplaceService<IRegistrableDomainPolicy>(services, new TestRegistrableDomainPolicy());
            });
        }

        private static void ReplaceService<TService>(IServiceCollection services, TService implementation)
            where TService : class
        {
            var descriptor = services.SingleOrDefault(service => service.ServiceType == typeof(TService));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(implementation);
        }
    }

    private sealed class TestRegistrableDomainPolicy : IRegistrableDomainPolicy
    {
        public bool IsSame(string firstHost, string secondHost)
        {
            return string.Equals(GetRegistrableDomain(firstHost), GetRegistrableDomain(secondHost), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRegistrableDomain(string host)
        {
            var labels = host.TrimEnd('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
            return labels.Length < 2 ? host : string.Join('.', labels[^2..]);
        }
    }
}
