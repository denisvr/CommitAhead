namespace CommitAhead.Api.Security;

/// <summary>
/// Exempts an action from CSRF validation. /auth/login carries this because it runs before any
/// session exists, so there is no CSRF cookie yet to validate against. ApiCatchAllController and
/// AuthCatchAllController carry it because a 404 response never performs a state change — without
/// it, a POST/PUT/PATCH/DELETE to a genuinely unmatched route would 400 (CSRF rejection) instead
/// of 404, since CsrfMiddleware runs before the controller action executes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class SkipCsrfAttribute : Attribute
{
}
