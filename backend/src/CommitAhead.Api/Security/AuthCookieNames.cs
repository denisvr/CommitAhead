namespace CommitAhead.Api.Security;

internal static class AuthCookieNames
{
    public const string AccessToken = "commitahead_access";
    public const string RefreshToken = "commitahead_refresh";
    public const string PkceState = "commitahead_pkce_state";

    /// <summary>
    /// Set once at login, never refreshed. Its value is a Data Protection-sealed session-start
    /// timestamp (see SessionStartToken) — /auth/refresh decrypts it and computes elapsed time
    /// explicitly, so the ADR-0006 7-day absolute timeout holds even against a non-browser client
    /// replaying a captured cookie past its own MaxAge (which only a real browser enforces).
    /// </summary>
    public const string SessionStarted = "commitahead_session_started";
}
