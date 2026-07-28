using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Auth;

[ApiController]
[AllowAnonymous]
[Route("auth/refresh")]
public sealed class RefreshController : ControllerBase
{
    private readonly RefreshUseCase _useCase;

    public RefreshController(RefreshUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.ContainsKey(AuthCookieNames.SessionStarted))
        {
            AuthCookieWriter.ClearSessionCookies(Response);
            return Unauthorized();
        }

        if (!Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var result = await _useCase.ExecuteAsync(refreshToken, cancellationToken);
        if (!result.IsAllowed || result.Tokens is null)
        {
            AuthCookieWriter.ClearSessionCookies(Response);
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        AuthCookieWriter.SetSessionCookies(Response, result.Tokens);

        return NoContent();
    }
}
