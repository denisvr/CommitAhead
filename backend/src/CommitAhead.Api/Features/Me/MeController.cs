using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Me;

// The contract requires every MVC operation to declare its authorization explicitly — the
// fallback policy protects mistakes, the attribute makes intent reviewable. [Authorize] resolves
// AuthorizationOptions.DefaultPolicy, which AddCommitAheadAuthentication sets to the same
// authenticated-and-enabled-user policy as FallbackPolicy, so this does not bypass
// EnabledUserRequirement (EnabledUserPolicyTests proves it).
[ApiController]
[Authorize]
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
