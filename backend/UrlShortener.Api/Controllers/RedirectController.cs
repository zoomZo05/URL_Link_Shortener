using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("")]
public sealed class RedirectController(ILinkService linkService) : ControllerBase
{
    [HttpGet("{code}")]
    public IActionResult RedirectToOriginal(string code)
    {
        var resolution = linkService.ResolveAndRecordAccess(code, Request.Headers.UserAgent.ToString());
        if (resolution.Status == LinkAccessStatus.NotFound)
        {
            return NotFound();
        }

        if (resolution.Status == LinkAccessStatus.Disabled)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        return Redirect(resolution.OriginalUrl!);
    }
}
