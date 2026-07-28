using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.Features.Auth;

[ApiController]
[AllowAnonymous]
[Route("auth/login")]
public sealed class LoginController : ControllerBase
{
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
}

public sealed record LoginRequest(string Email);

public sealed record LoginResponse(string Message);
