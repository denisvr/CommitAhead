using CommitAhead.Api.Security;
using CommitAhead.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Auth;

[ApiController]
[AllowAnonymous]
[Route("auth/callback")]
public sealed class CallbackController : ControllerBase
{
    private readonly CallbackUseCase _useCase;

    public CallbackController(CallbackUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookieNames.PkceState, out var codeVerifier) || string.IsNullOrEmpty(codeVerifier))
        {
            return BadRequest();
        }

        AuthCookieWriter.ClearPkceStateCookie(Response);

        var result = await _useCase.ExecuteAsync(code, codeVerifier, cancellationToken);
        if (!result.IsAllowed || result.Tokens is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        AuthCookieWriter.SetSessionCookies(Response, result.Tokens);
        AuthCookieWriter.SetSessionStartedMarker(Response);

        return Redirect("/");
    }
}
