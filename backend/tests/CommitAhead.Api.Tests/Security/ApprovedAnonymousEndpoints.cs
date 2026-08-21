namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// The reviewed inventory of endpoints allowed to skip authentication. Every entry is a deliberate
/// decision, not an accident, and adding one is a security review — the whole point of the
/// inventory is that a new anonymous endpoint cannot appear silently. Identifiers use the
/// verifier's own format, "&lt;HTTP method&gt; /&lt;route template&gt;", with "*" for an action that
/// declares no method constraint.
/// </summary>
public static class ApprovedAnonymousEndpoints
{
    public static readonly string[] All =
    [
        // Liveness probe. Returns a fixed status string and reads nothing.
        "GET /api/health",

        // The login flow itself cannot require a session. Each of these is anonymous by necessity:
        // login sends the magic link, callback exchanges the PKCE code for the first session,
        // refresh runs when the access token has already expired, logout must work even when the
        // access token is no longer valid, and csrf issues the token the SPA echoes back.
        "POST /auth/login",
        "GET /auth/callback",
        "POST /auth/refresh",
        "POST /auth/logout",
        "GET /auth/csrf",

        // E2E-only session minting (docs/testing/strategy.md 7.3). Present in the assembly, so the
        // verifier sees it, but it checks the environment name before reading any configuration and
        // 404s everywhere except the E2E stack — E2ESessionEndpointTests and E2EConfigurationGuard
        // own that guarantee. It is also excluded from the generated OpenAPI document.
        "POST /auth/e2e/session",

        // Unmatched /api and /auth requests must 404 rather than 401, and must not fall through to
        // the SPA shell. The fallback authorization policy applies even to requests that match no
        // endpoint, which is why these need [AllowAnonymous] to answer at all. Neither reads state.
        "* /api/{**catchall}",
        "* /auth/{**catchall}",
    ];
}
