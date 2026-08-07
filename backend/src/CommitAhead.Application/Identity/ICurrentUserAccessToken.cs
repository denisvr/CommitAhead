namespace CommitAhead.Application.Identity;

/// <summary>
/// The current request's own Supabase-issued access token — the exact same JWT already validated
/// to authenticate this request against our own API (see AuthenticationServiceCollectionExtensions).
/// Forwarded to Supabase Storage's REST API so its own RLS isolates by <c>auth.uid()</c> natively,
/// without a service-role secret (ADR-0018). Deliberately separate from <see cref="ICurrentUser"/>
/// so nothing about that interface's existing shape or its test doubles needs to change.
/// </summary>
public interface ICurrentUserAccessToken
{
    string Value { get; }
}
