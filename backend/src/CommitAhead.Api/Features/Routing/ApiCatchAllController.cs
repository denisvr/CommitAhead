using CommitAhead.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.Routing;

/// <summary>
/// Catches any /api/* request that no other controller matches and returns a real 404 instead of
/// letting it fall through to the SPA shell. [AllowAnonymous] is required: the secure-by-default
/// fallback authorization policy (see AuthenticationServiceCollectionExtensions) applies even to
/// requests that match no endpoint at all, so without this a genuinely unmatched /api route would
/// 401 instead of 404. [SkipCsrf] is required too, or a POST/PUT/PATCH/DELETE to an unmatched
/// route would 400 instead of 404. ASP.NET Core routing prefers any more specific controller route
/// (e.g. api/health) over this catch-all regardless of registration order.
/// </summary>
[ApiController]
[AllowAnonymous]
[SkipCsrf]
[Route("api")]
public sealed class ApiCatchAllController : ControllerBase
{
    [Route("{**catchall}")]
    public IActionResult CatchAll(string catchall) => NotFound();
}
