using CommitAhead.Application.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

/// <summary>Always answers "no EvidenceLinks" — matches production reality until Phase 4 adds a creation command.</summary>
public sealed class FakeEvidenceLinkQuery : IEvidenceLinkQuery
{
    public decimal Demand { get; set; }

    public bool AnyTargeting { get; set; }

    public Task<decimal> GetDemandAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken) => Task.FromResult(Demand);

    public Task<bool> AnyTargetingStudyItemAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken) => Task.FromResult(AnyTargeting);
}
