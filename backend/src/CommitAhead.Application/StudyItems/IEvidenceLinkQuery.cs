namespace CommitAhead.Application.StudyItems;

/// <summary>
/// Both queries hit the same EvidenceLink table (model.md §7) — always empty today since no
/// command creates EvidenceLinks yet (Phase 4), but both are real queries, not hardcoded stand-ins
/// (docs/roadmap.md Phase 1).
/// </summary>
public interface IEvidenceLinkQuery
{
    /// <summary>Demand is min(Σ weight targeting the item, 5) (model.md).</summary>
    Task<decimal> GetDemandAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken);

    /// <summary>The EvidenceLink half of the hard-delete guard (model.md invariant 2).</summary>
    Task<bool> AnyTargetingStudyItemAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken);
}
