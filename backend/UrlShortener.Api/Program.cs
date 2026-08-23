using UrlShortener.Application.Abstractions;
using UrlShortener.Application.Links;
using UrlShortener.Api;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.ShortCodes;
using UrlShortener.Infrastructure.Domains;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers();
builder.Services.Configure<ShortLinkOptions>(builder.Configuration.GetSection(ShortLinkOptions.SectionName));
var allowedOrigins = builder.Configuration
    .GetSection($"{ShortLinkOptions.SectionName}:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddSingleton<IUrlRepository, InMemoryUrlRepository>();
builder.Services.AddSingleton<IPlatformDetector, UserAgentPlatformDetector>();
builder.Services.AddSingleton<IRegistrableDomainPolicy>(serviceProvider =>
    new PublicSuffixRegistrableDomainPolicy(
        builder.Configuration,
        new HttpClient(),
        serviceProvider.GetRequiredService<ILogger<Nager.PublicSuffix.RuleProviders.SimpleHttpRuleProvider>>()));
builder.Services.AddSingleton<IShortCodeGenerator>(CreateShortCodeGenerator);
builder.Services.AddSingleton<ILinkService, LinkService>();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        logger.LogError(exception, "Unhandled exception while processing {RequestMethod} {RequestPath}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.",
            detail: "The server could not complete the request.").ExecuteAsync(context);
    });
});

app.UseCors("Frontend");
app.MapControllers();

app.Run();

static IShortCodeGenerator CreateShortCodeGenerator(IServiceProvider serviceProvider)
{
    var options = serviceProvider.GetRequiredService<IOptions<ShortLinkOptions>>().Value;
    return new SecureShortCodeGenerator(options.CodeLength);
}

public partial class Program;
