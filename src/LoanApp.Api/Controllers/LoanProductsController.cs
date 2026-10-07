using LoanApp.Application;
using LoanApp.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.Controllers;

[ApiController]
[Authorize]
[Tags("Loan products")]
[Route("api/v1")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly LoanApplicationService _service;

    public LoanProductsController(LoanApplicationService service)
    {
        _service = service;
    }

    [HttpGet("loan-products")]
    [ProducesResponseType(typeof(IReadOnlyList<LoanProductDto>), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(_service.GetProducts());
}
