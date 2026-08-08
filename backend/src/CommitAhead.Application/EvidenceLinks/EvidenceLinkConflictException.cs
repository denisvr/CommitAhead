namespace CommitAhead.Application.EvidenceLinks;

/// <summary>
/// Thrown by <c>IEvidenceLinkRepository.AddAsync</c> when the database's own unique index on
/// (SourceType, SourceId, TargetStudyItemId) rejects a concurrent duplicate — the last-resort
/// guard beneath ApplyAnalysisDraftUseCase's own pre-check (ExistsAsync). Infrastructure maps only
/// this exact named constraint's violation to this exception; every other database failure
/// propagates unchanged.
/// </summary>
public sealed class EvidenceLinkConflictException : Exception
{
    public EvidenceLinkConflictException()
        : base("An EvidenceLink already exists for this source and target.")
    {
    }
}
