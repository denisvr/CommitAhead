using CommitAhead.Domain;

namespace CommitAhead.Domain.JobAnalyses;

/// <summary>
/// A record created for a specific job posting (CONTEXT.md) — an evidence source (ADR-0002),
/// never itself a StudyItem or placed in the study queue. Holds a <see cref="JobSource"/> (fixed
/// at creation — no use case describes changing it; a different source means a new JobAnalysis)
/// plus <see cref="Requirements"/> and <see cref="Gaps"/> extracted from it.
///
/// Requirements/Gaps are added and removed one at a time, not replaced as a whole collection like
/// ProfessionalProfile's editor-driven children — they arrive one accepted-proposal at a time from
/// Phase 4's AI/AnalysisDraft pipeline (docs/domain/use-cases.md §3), not as a user-submitted batch.
/// No Phase 3 use case creates a JobRequirement or JobGap directly.
/// </summary>
public sealed class JobAnalysis
{
    private readonly List<JobRequirement> _requirements = [];
    private readonly List<JobGap> _gaps = [];

    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string Title { get; private set; }
    public JobSource JobSource { get; }
    public string? NotesMarkdown { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<JobRequirement> Requirements => _requirements;
    public IReadOnlyList<JobGap> Gaps => _gaps;

    public JobAnalysis(Guid id, Guid ownerUserId, string title, JobSource jobSource, string? notesMarkdown, DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (jobSource is null)
        {
            throw new DomainValidationException("JobSource is required.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        Title = TextValidation.RequireNonBlank(title, nameof(title), ValidationLimits.TitleMaxLength);
        JobSource = jobSource;
        NotesMarkdown = TextValidation.TrimToNullOrValidate(notesMarkdown, nameof(notesMarkdown), ValidationLimits.NotesMarkdownMaxLength);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>Validates both fields before assigning either, so a rejected update leaves the analysis completely unchanged. JobSource is never included — immutable after creation.</summary>
    public void Update(string title, string? notesMarkdown, DateTime updatedAtUtc)
    {
        var validatedTitle = TextValidation.RequireNonBlank(title, nameof(title), ValidationLimits.TitleMaxLength);
        var validatedNotes = TextValidation.TrimToNullOrValidate(notesMarkdown, nameof(notesMarkdown), ValidationLimits.NotesMarkdownMaxLength);

        Title = validatedTitle;
        NotesMarkdown = validatedNotes;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AddRequirement(JobRequirement requirement, DateTime updatedAtUtc)
    {
        EnsureCanAdd(_requirements, requirement, r => r.Id, nameof(requirement));

        _requirements.Add(requirement);
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Also removes any JobGap referencing this requirement (invariant 16, kept true by construction) — both collections are computed before either is assigned, so a failure here is impossible to leave half-applied.</summary>
    public void RemoveRequirement(Guid id, DateTime updatedAtUtc)
    {
        var remainingRequirements = _requirements.Where(r => r.Id != id).ToList();
        var remainingGaps = _gaps.Where(g => g.RequirementId != id).ToList();

        _requirements.Clear();
        _requirements.AddRange(remainingRequirements);
        _gaps.Clear();
        _gaps.AddRange(remainingGaps);
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Validates RequirementId exists in Requirements (invariant 16) before adding — see JobGapMatchLevel for why invariant 17 needs no separate check here.</summary>
    public void AddGap(JobGap gap, DateTime updatedAtUtc)
    {
        EnsureCanAdd(_gaps, gap, g => g.Id, nameof(gap));

        if (_requirements.All(r => r.Id != gap.RequirementId))
        {
            throw new DomainValidationException("gap.RequirementId must reference a requirement on this JobAnalysis.");
        }

        _gaps.Add(gap);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RemoveGap(Guid id, DateTime updatedAtUtc)
    {
        _gaps.RemoveAll(g => g.Id == id);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void EnsureCanAdd<T>(List<T> existing, T candidate, Func<T, Guid> idSelector, string paramName)
    {
        if (candidate is null)
        {
            throw new DomainValidationException($"{paramName} must not be null.");
        }

        var id = idSelector(candidate);
        if (id == Guid.Empty)
        {
            throw new DomainValidationException($"{paramName} must have a non-empty Id.");
        }

        if (existing.Any(e => idSelector(e) == id))
        {
            throw new DomainValidationException($"{paramName} Id must not duplicate an existing entry.");
        }
    }
}
