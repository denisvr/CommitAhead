using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Auth;

[ApiController]
[AllowAnonymous]
[Route("auth/logout")]
public sealed class LogoutController : ControllerBase
{
    private readonly LogoutUseCase _useCase;

    public LogoutController(LogoutUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var accessToken) && !string.IsNullOrEmpty(accessToken))
        {
            try
            {
                await _useCase.ExecuteAsync(accessToken, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Best-effort revoke (ADR-0006): logout must still clear cookies even if the
                // access token was already expired/invalid when Supabase received it.
            }
        }

        AuthCookieWriter.ClearSessionCookies(Response);

        return NoContent();
    }
}
