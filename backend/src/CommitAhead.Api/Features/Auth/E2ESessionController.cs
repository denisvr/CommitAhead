using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommitAhead.Api.Features.Auth;

/// <summary>
/// E2E-only replacement for the real Supabase magic-link callback (docs/testing/strategy.md
/// §7.3). Mints a locally-signed session for the one seeded E2E user and writes exactly the
/// cookies CallbackController writes, so everything downstream of login is exercised unchanged.
/// Never reachable outside the E2E environment — checked first, before any E2E configuration is
/// even read — and excluded from the generated OpenAPI document, since it has no production
/// meaning and must never appear in the frontend's generated client. Accepts no request body,
/// query string, or header value: the minted identity comes only from trusted E2E configuration,
/// never from the caller.
/// </summary>
[ApiController]
[AllowAnonymous]
[SkipCsrf]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("auth/e2e/session")]
public sealed class E2ESessionController : ControllerBase
{
    private const string E2EEnvironmentName = "E2E";
    private const int SessionEffectiveLifetimeMinutes = 10; // strictly under the 15-minute iat cap enforced elsewhere

    private readonly IHostEnvironment _environment;
    private readonly E2EOptions _e2eOptions;
    private readonly SessionStartToken _sessionStartToken;

    public E2ESessionController(IHostEnvironment environment, IOptions<E2EOptions> e2eOptions, SessionStartToken sessionStartToken)
    {
        _environment = environment;
        _e2eOptions = e2eOptions.Value;
        _sessionStartToken = sessionStartToken;
    }

    [HttpPost]
    public IActionResult Post()
    {
        // The environment check runs before anything else in this action — E2E configuration is
        // read only once this has passed, so a request reaching this action under any other
        // environment name learns nothing about whether E2E configuration even exists.
        if (!_environment.IsEnvironment(E2EEnvironmentName))
        {
            return NotFound();
        }

        var signingKey = _e2eOptions.SigningKey!;
        var issuer = _e2eOptions.Issuer!;
        var supabaseUserId = _e2eOptions.SupabaseUserId!;

        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(SessionEffectiveLifetimeMinutes);

        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("sub", supabaseUserId),
            new Claim("iat", new DateTimeOffset(issuedAtUtc).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        // The JwtSecurityToken constructor writes both notBefore and expires into the token's own
        // "nbf" and "exp" claims — no separate claim needed for either.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: "authenticated",
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = handler.WriteToken(token);

        // The refresh token is opaque to this endpoint and to the app — it is only ever handed
        // back, unmodified, to the external stub's deterministic refresh endpoint, which returns
        // a fresh access token unconditionally for any well-formed request. No value here is a
        // real Supabase credential.
        var tokens = new SupabaseTokenResult(accessToken, "e2e-seed-refresh-token", new DateTimeOffset(expiresAtUtc), supabaseUserId);

        AuthCookieWriter.SetSessionCookies(Response, tokens);
        AuthCookieWriter.SetSessionStartedMarker(Response, _sessionStartToken.Protect(DateTimeOffset.UtcNow));

        return NoContent();
    }
}
