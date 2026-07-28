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
        // Cookies are cleared in `finally` — not just after the try block — so they are cleared
        // even if something unexpected escapes LogoutUseCase (which already swallows a failed
        // Supabase revoke on its own; this is defense-in-depth, not the primary safety net).
        try
        {
            if (Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var accessToken) && !string.IsNullOrEmpty(accessToken))
            {
                await _useCase.ExecuteAsync(accessToken, cancellationToken);
            }
        }
        finally
        {
            AuthCookieWriter.ClearSessionCookies(Response);
        }

        return NoContent();
    }
}
