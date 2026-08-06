using CommitAhead.Domain;

namespace CommitAhead.Domain.JobAnalyses;

/// <summary>A gap between a JobRequirement and the user's ProfessionalProfile (CONTEXT.md). Immutable, like JobRequirement. <see cref="RequirementId"/>'s existence in the owning JobAnalysis (invariant 16) is checked by <see cref="JobAnalysis.AddGap"/>, not here — this type has no access to sibling requirements.</summary>
public sealed class JobGap
{
    public Guid Id { get; }
    public Guid RequirementId { get; }
    public JobGapMatchLevel MatchLevel { get; }
    public JobGapSeverity Severity { get; }
    public string Rationale { get; }

    public JobGap(Guid id, Guid requirementId, JobGapMatchLevel matchLevel, JobGapSeverity severity, string rationale)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (requirementId == Guid.Empty)
        {
            throw new DomainValidationException("RequirementId is required.");
        }

        Id = id;
        RequirementId = requirementId;
        MatchLevel = matchLevel;
        Severity = severity;
        Rationale = TextValidation.RequireNonBlank(rationale, nameof(rationale), ValidationLimits.GapRationaleMaxLength);
    }
}
