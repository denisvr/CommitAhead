namespace CommitAhead.Api.Security;

internal static class AuthCookieNames
{
    public const string AccessToken = "commitahead_access";
    public const string RefreshToken = "commitahead_refresh";
    public const string PkceState = "commitahead_pkce_state";

    /// <summary>
    /// Set once at login, never refreshed. Its own expiry (7 days) is what enforces the ADR-0006
    /// absolute session timeout — /auth/refresh refuses to rotate tokens once this is gone, even
    /// if the underlying Supabase refresh token would still work.
    /// </summary>
    public const string SessionStarted = "commitahead_session_started";
}
