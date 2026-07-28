using CommitAhead.Application.Identity;

namespace CommitAhead.Api.Identity;

/// <summary>
/// Scoped per-request. Set once by EnabledUserMiddleware after the ADR-0015 check passes;
/// unset (default Guid/empty email) for anonymous requests or requests that never reach an
/// authenticated route.
/// </summary>
internal sealed class CurrentUserAccessor : ICurrentUser
{
    public Guid UserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public void Set(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}
