using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Me;

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
