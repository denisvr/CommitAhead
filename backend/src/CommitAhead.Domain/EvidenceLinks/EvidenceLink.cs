namespace CommitAhead.Domain.EvidenceLinks;

/// <summary>
/// Confirmed evidence that a source (CVPresentation/JobAnalysis/InterviewNote) supports
/// prioritising a StudyItem — contributes to its Demand (docs/domain/model.md invariant 7-9).
/// No proposal lifecycle: existence means active. Created only from an accepted LinkProposal
/// (ADR-0004) — that command does not exist yet (Phase 4), so nothing constructs this today
/// except tests; the schema exists now so the ranked-queue Demand query has a real table to
/// join against (docs/roadmap.md Phase 1).
/// </summary>
public sealed class EvidenceLink
{
    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public EvidenceSourceType SourceType { get; }
    public Guid SourceId { get; }
    public Guid TargetStudyItemId { get; }
    public decimal Weight { get; }
    public string Rationale { get; }
    public DateTime CreatedAtUtc { get; }

    public EvidenceLink(Guid id, Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, Guid targetStudyItemId, decimal weight, string rationale, DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        }

        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("SourceId is required.", nameof(sourceId));
        }

        if (targetStudyItemId == Guid.Empty)
        {
            throw new ArgumentException("TargetStudyItemId is required.", nameof(targetStudyItemId));
        }

        if (weight is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be in [0,5].");
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException("Rationale is required.", nameof(rationale));
        }

        Id = id;
        OwnerUserId = ownerUserId;
        SourceType = sourceType;
        SourceId = sourceId;
        TargetStudyItemId = targetStudyItemId;
        Weight = weight;
        Rationale = rationale.Trim();
        CreatedAtUtc = createdAtUtc;
    }
}
