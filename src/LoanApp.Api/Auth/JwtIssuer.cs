using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace LoanApp.Api.Auth;

public sealed class JwtIssuer
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly int _minutes;

    public JwtIssuer(string issuer, string audience, string signingKey, int minutes)
    {
        _issuer = issuer;
        _audience = audience;
        _key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey));
        _minutes = minutes <= 0 ? 60 : minutes;
    }

    public (string Token, int ExpiresInSeconds) Create(string userId, string email, string name, string role)
    {
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role)
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_minutes),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _minutes * 60);
    }
}
