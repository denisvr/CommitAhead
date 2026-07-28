using System.Text.RegularExpressions;
using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.Features.Auth;

[ApiController]
[AllowAnonymous]
[Route("auth/login")]
public sealed partial class LoginController : ControllerBase
{
    // RFC 5321 max mailbox length. Format is a basic structural check (not full RFC 5322) —
    // provisioning is admin-driven, so this only needs to reject obviously malformed input.
    private const int MaxEmailLength = 320;

    private readonly LoginUseCase _useCase;

    public LoginController(LoginUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    [SkipCsrf]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Post([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || request.Email.Length > MaxEmailLength
            || !EmailFormatRegex().IsMatch(request.Email))
        {
            return BadRequest();
        }

        var codeVerifier = await _useCase.ExecuteAsync(request.Email, cancellationToken);

        Response.Cookies.Append(AuthCookieNames.PkceState, codeVerifier, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(15),
        });

        return Ok(new LoginResponse("If that email is registered, a sign-in link has been sent."));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormatRegex();
}

public sealed record LoginRequest(string Email);

public sealed record LoginResponse(string Message);
