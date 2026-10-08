using LoanApp.Api.Auth;
using LoanApp.Application.Contracts;
using LoanApp.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApp.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Tags("Auth")]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtIssuer? _issuer;
    private readonly IUserRepository _users;

    public AuthController(IUserRepository users, IServiceProvider services)
    {
        _users = users;
        _issuer = services.GetService<JwtIssuer>();
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (_issuer is null)
        {
            return StatusCode(StatusCodes.Status404NotFound, new ApiError("auth_unavailable", "Local JWT login is not enabled."));
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new ApiError("invalid_credentials", "Email or password is incorrect."));
        }

        var user = await _users.FindByEmailAsync(request.Email.Trim(), ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return Unauthorized(new ApiError("invalid_credentials", "Email or password is incorrect."));
        }

        var (token, expiresIn) = _issuer.Create(user.Id, user.Email, user.DisplayName, user.Role.ToString());
        return Ok(new LoginResponse(token, "Bearer", expiresIn, user.Id, user.Email, user.DisplayName, user.Role.ToString()));
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserId,
    string Email,
    string Name,
    string Role);
