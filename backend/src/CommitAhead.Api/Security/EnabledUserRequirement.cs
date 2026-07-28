using Microsoft.AspNetCore.Authorization;

namespace CommitAhead.Api.Security;

/// <summary>
/// ADR-0015: the authenticated Supabase `sub` must resolve to an existing, enabled application
/// User. Applied only to protected resources via the authorization fallback policy — endpoints
/// marked [AllowAnonymous] (login, callback, refresh, logout, csrf) never evaluate this
/// requirement, so a disabled/unknown user can still complete those flows.
/// </summary>
internal sealed class EnabledUserRequirement : IAuthorizationRequirement
{
}
