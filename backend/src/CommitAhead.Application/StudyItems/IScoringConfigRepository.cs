using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

/// <summary>
/// ScoringConfigOverride is an operational record, not a domain aggregate (docs/domain/model.md)
/// — at most one row per user; absence means code defaults (ScoringWeights.Default) apply.
/// </summary>
public interface IScoringConfigRepository
{
    Task<ScoringWeights?> GetOverrideAsync(Guid ownerUserId, CancellationToken cancellationToken);

    /// <summary>Creates the row on first save for this user, or replaces the existing one.</summary>
    Task SetOverrideAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken);

    /// <summary>Removes the override row, if any, so code defaults apply again.</summary>
    Task ResetAsync(Guid ownerUserId, CancellationToken cancellationToken);
}
