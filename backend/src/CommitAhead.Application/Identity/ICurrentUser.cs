namespace CommitAhead.Application.Identity;

/// <summary>
/// The authenticated user for the current request, populated by the ADR-0015 enabled-user
/// check. Only meaningful once that check has run; unauthenticated requests never resolve one.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    string Email { get; }
}
