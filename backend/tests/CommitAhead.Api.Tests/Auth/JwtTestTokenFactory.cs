using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Locally-signed JWTs for API tests, per docs/testing/strategy.md — never a real Supabase
/// token, never a real network call.
/// </summary>
public static class JwtTestTokenFactory
{
    public static string CreateAccessToken(string subject, DateTime? expiresUtc = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(AuthTestWebApplicationFactory.SigningKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AuthTestWebApplicationFactory.TestIssuer,
            audience: "authenticated",
            claims: [new Claim("sub", subject)],
            expires: expiresUtc ?? DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return handler.WriteToken(token);
    }
}
