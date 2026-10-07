using System.Security.Claims;
using System.Text.Encodings.Web;
using LoanApp.Application.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LoanApp.Api.Auth;

public sealed class DevelopmentAuthOptions : AuthenticationSchemeOptions
{
}

public sealed class DevelopmentAuthHandler : AuthenticationHandler<DevelopmentAuthOptions>
{
    public const string SchemeName = "Development";

    public DevelopmentAuthHandler(
        IOptionsMonitor<DevelopmentAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = "dev-user";
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Name, userId)
        ], SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue("oid")
                ?? user?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("Authenticated user id is missing.");
        }
    }
}
