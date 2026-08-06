using CommitAhead.Domain;

namespace CommitAhead.Domain.JobAnalyses;

/// <summary>A single requirement extracted from a job posting (CONTEXT.md). Immutable — created once, by an accepted proposal, and either present or removed, never edited in place.</summary>
public sealed class JobRequirement
{
    public Guid Id { get; }
    public string Text { get; }
    public JobRequirementKind Kind { get; }
    public JobRequirementPriority Priority { get; }
    public string SourceExcerpt { get; }

    public JobRequirement(Guid id, string text, JobRequirementKind kind, JobRequirementPriority priority, string sourceExcerpt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        Id = id;
        Text = TextValidation.RequireNonBlank(text, nameof(text), ValidationLimits.RequirementTextMaxLength);
        Kind = kind;
        Priority = priority;
        SourceExcerpt = TextValidation.RequireNonBlank(sourceExcerpt, nameof(sourceExcerpt), ValidationLimits.SourceExcerptMaxLength);
    }
}
