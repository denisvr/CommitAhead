using CommitAhead.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Routing;

/// <summary>
/// Catches any /auth/* request that no other controller matches and returns a real 404 instead of
/// letting it fall through to the SPA shell. See ApiCatchAllController for why [AllowAnonymous]
/// and [SkipCsrf] are both required.
/// </summary>
[ApiController]
[AllowAnonymous]
[SkipCsrf]
[Route("auth")]
public sealed class AuthCatchAllController : ControllerBase
{
    [Route("{**catchall}")]
    public IActionResult CatchAll(string catchall) => NotFound();
}
