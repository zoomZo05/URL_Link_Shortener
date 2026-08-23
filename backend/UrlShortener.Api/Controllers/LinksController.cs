using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Abstractions;
using UrlShortener.Api.Contracts;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/links")]
public sealed class LinksController(
    ILinkService linkService,
    IOptions<ShortLinkOptions> options,
    ILogger<LinksController> logger) : ControllerBase
{
    [HttpPost]
    public ActionResult<LinkResponse> Create(CreateLinkRequest request)
    {
        var result = linkService.Create(
            request.OriginalUrl ?? string.Empty,
            request.CustomAlias,
            request.PlatformOverrides is null
                ? null
                : new PlatformOverrides(request.PlatformOverrides.Ios, request.PlatformOverrides.Android));

        if (result is InvalidLinkInput invalidInput)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid link", Detail = invalidInput.Error });
        }

        if (result is CustomAliasAlreadyInUse aliasConflict)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Short code conflict",
                Detail = $"CustomAlias '{aliasConflict.Alias}' is already in use."
            });
        }

        if (result is ShortCodeGenerationUnavailable)
        {
            logger.LogError("Short-code generation was exhausted while creating a link.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Short code generation unavailable",
                Detail = "A unique short code could not be generated. Please retry."
            });
        }

        var created = (LinkCreated)result;
        var response = ToResponse(created.Link.GetSnapshot(), options.Value.BaseUrl);
        return Created($"/api/links/{created.Link.ShortCode}/stats", response);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<LinkResponse>> List()
    {
        return Ok(linkService.List().Select(link => ToResponse(link, options.Value.BaseUrl)));
    }

    [HttpGet("{code}/stats")]
    public ActionResult<LinkResponse> GetStats(string code)
    {
        var link = linkService.Get(code);
        if (link is null || link.IsDeleted)
        {
            return NotFound();
        }

        return Ok(ToResponse(link, options.Value.BaseUrl));
    }

    [HttpPatch("{code}/status")]
    public IActionResult UpdateStatus(string code, [FromBody] StatusRequest request)
    {
        if (linkService.UpdateStatus(code, request.IsActive))
        {
            return NoContent();
        }

        return NotFound();
    }

    [HttpDelete("{code}")]
    public IActionResult Delete(string code)
    {
        if (linkService.Delete(code))
        {
            return NoContent();
        }

        return NotFound();
    }

    private static LinkResponse ToResponse(ShortLinkSnapshot link, string baseUrl)
    {
        return new LinkResponse(
            link.Id,
            link.ShortCode,
            $"{baseUrl.TrimEnd('/')}/{link.ShortCode}",
            link.OriginalUrl,
            link.PlatformOverrides,
            link.ClickCount,
            link.IsActive && !link.IsDeleted,
            link.CreatedAtUtc,
            link.LastAccessedAtUtc);
    }
}

public sealed class StatusRequest
{
    public bool IsActive { get; init; }
}
