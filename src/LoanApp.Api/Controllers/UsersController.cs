using LoanApp.Application;
using LoanApp.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Tags("Users")]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly LoanApplicationService _service;

    public UsersController(LoanApplicationService service)
    {
        _service = service;
    }

    [HttpGet("applicants")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicantUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Applicants(CancellationToken ct) =>
        Ok(await _service.ListApplicantsAsync(ct));
}
