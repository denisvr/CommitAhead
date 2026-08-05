using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

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
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (sourceId == Guid.Empty)
        {
            throw new DomainValidationException("SourceId is required.");
        }

        if (targetStudyItemId == Guid.Empty)
        {
            throw new DomainValidationException("TargetStudyItemId is required.");
        }

        if (weight is < 0 or > 5)
        {
            throw new DomainValidationException("Weight must be in [0,5].");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        SourceType = sourceType;
        SourceId = sourceId;
        TargetStudyItemId = targetStudyItemId;
        Weight = weight;
        Rationale = TextValidation.RequireNonBlank(rationale, nameof(rationale), ValidationLimits.EvidenceLinkRationaleMaxLength);
        CreatedAtUtc = createdAtUtc;
    }
}
