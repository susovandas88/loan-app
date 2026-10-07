using LoanApp.Application.Contracts;
using LoanApp.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.Controllers;

/// <summary>
/// Development-only file receiver used when Storage:Mode is Local.
/// The SPA PUTs the file to the short-lived URL from POST .../documents/upload-url.
/// Not available outside Development.
/// </summary>
[ApiController]
[AllowAnonymous]
[Tags("Development")]
[Route("api/v1/dev/uploads")]
public sealed class DevUploadsController : ControllerBase
{
    private readonly LocalUploadTicketStore? _tickets;
    private readonly LocalBlobStorageService? _blobs;
    private readonly IWebHostEnvironment _env;

    public DevUploadsController(IWebHostEnvironment env, IServiceProvider services)
    {
        _env = env;
        _tickets = services.GetService<LocalUploadTicketStore>();
        _blobs = services.GetService<IBlobStorageService>() as LocalBlobStorageService;
    }

    [HttpPut("{token}")]
    [HttpPost("{token}")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(string token, CancellationToken ct)
    {
        if (!_env.IsDevelopment() || _tickets is null || _blobs is null)
        {
            return NotFound();
        }

        if (!_tickets.TryGet(token, out var ticket))
        {
            return NotFound();
        }

        if (ticket.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Unauthorized();
        }

        await _blobs.SaveAsync(ticket.BlobPath, Request.Body, ct);
        _tickets.Complete(token);
        return StatusCode(StatusCodes.Status201Created);
    }
}
