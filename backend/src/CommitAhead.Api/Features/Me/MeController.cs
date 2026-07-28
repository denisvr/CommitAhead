using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Me;

// Secure by default: no [Authorize] needed — the authorization fallback policy already requires
// an authenticated, enabled user for any endpoint without [AllowAnonymous]. An explicit [Authorize]
// here would use AuthorizationOptions.DefaultPolicy instead of FallbackPolicy, bypassing
// EnabledUserRequirement entirely.
[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly GetCurrentUserUseCase _useCase;

    public MeController(GetCurrentUserUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpGet]
    public ActionResult<MeResponse> Get()
    {
        var result = _useCase.Execute();
        return Ok(new MeResponse(result.Email));
    }
}

public sealed record MeResponse(string Email);
