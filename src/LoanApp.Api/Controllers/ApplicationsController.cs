using LoanApp.Application;
using LoanApp.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.Controllers;

[ApiController]
[Authorize]
[Tags("Applications")]
[Route("api/v1/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly LoanApplicationService _service;

    public ApplicationsController(LoanApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await _service.ListAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateDraftAsync(id, request, ct));

    [HttpPost("{id:guid}/documents/upload-url")]
    [ProducesResponseType(typeof(UploadUrlResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadUrl(Guid id, [FromBody] UploadUrlRequest request, CancellationToken ct) =>
        Ok(await _service.CreateUploadUrlAsync(id, request, ct));

    [HttpPost("{id:guid}/documents/{documentId:guid}/complete")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(Guid id, Guid documentId, CancellationToken ct) =>
        Ok(await _service.CompleteUploadAsync(id, documentId, ct));

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        Request.Headers.TryGetValue("Idempotency-Key", out var key);
        return Ok(await _service.SubmitAsync(id, key.ToString(), ct));
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(ApplicationStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(Guid id, CancellationToken ct) =>
        Ok(await _service.GetStatusAsync(id, ct));
}
