using CommitAhead.Domain;

namespace CommitAhead.Domain.InterviewNotes;

/// <summary>
/// A structured record of a real interview (CONTEXT.md) — an evidence source (ADR-0002), never
/// itself a StudyItem. Optionally linked to a JobAnalysis; that reference is application-enforced
/// same-owner (invariant 29) and is nulled, never cascade-deleted, when the JobAnalysis is deleted
/// (invariant 19 — see <see cref="ClearJobAnalysisReference"/>).
/// </summary>
public sealed class InterviewNote
{
    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string Company { get; private set; }
    public string Role { get; private set; }
    public InterviewRound InterviewRound { get; private set; }
    public int SequenceNumber { get; private set; }
    public string? OtherLabel { get; private set; }
    public DateOnly Date { get; private set; }
    public IReadOnlyList<string> Questions { get; private set; }
    public IReadOnlyList<string> Gaps { get; private set; }
    public IReadOnlyList<string> Lessons { get; private set; }
    public Guid? JobAnalysisId { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }

    public InterviewNote(
        Guid id,
        Guid ownerUserId,
        string company,
        string role,
        InterviewRound interviewRound,
        int sequenceNumber,
        string? otherLabel,
        DateOnly date,
        IReadOnlyList<string> questions,
        IReadOnlyList<string> gaps,
        IReadOnlyList<string> lessons,
        Guid? jobAnalysisId,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        var validatedRound = TextValidation.ValidateDefined(interviewRound, nameof(interviewRound));

        Id = id;
        OwnerUserId = ownerUserId;
        Company = TextValidation.RequireNonBlank(company, nameof(company), ValidationLimits.ShortTextMaxLength);
        Role = TextValidation.RequireNonBlank(role, nameof(role), ValidationLimits.ShortTextMaxLength);
        InterviewRound = validatedRound;
        SequenceNumber = ValidateSequenceNumber(sequenceNumber);
        OtherLabel = ValidateOtherLabel(validatedRound, otherLabel);
        Date = ValidateDate(date);
        Questions = TextValidation.RequireEntries(questions, nameof(questions));
        Gaps = TextValidation.RequireEntries(gaps, nameof(gaps));
        Lessons = TextValidation.RequireEntries(lessons, nameof(lessons));
        JobAnalysisId = ValidateJobAnalysisId(jobAnalysisId);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>Validates every field into locals before assigning any of them, so a rejected update leaves the note completely unchanged.</summary>
    public void Update(
        string company,
        string role,
        InterviewRound interviewRound,
        int sequenceNumber,
        string? otherLabel,
        DateOnly date,
        IEnumerable<string> questions,
        IEnumerable<string> gaps,
        IEnumerable<string> lessons,
        Guid? jobAnalysisId,
        DateTime updatedAtUtc)
    {
        var validatedCompany = TextValidation.RequireNonBlank(company, nameof(company), ValidationLimits.ShortTextMaxLength);
        var validatedRole = TextValidation.RequireNonBlank(role, nameof(role), ValidationLimits.ShortTextMaxLength);
        var validatedRound = TextValidation.ValidateDefined(interviewRound, nameof(interviewRound));
        var validatedSequenceNumber = ValidateSequenceNumber(sequenceNumber);
        var validatedOtherLabel = ValidateOtherLabel(validatedRound, otherLabel);
        var validatedDate = ValidateDate(date);
        var validatedQuestions = TextValidation.RequireEntries(questions, nameof(questions));
        var validatedGaps = TextValidation.RequireEntries(gaps, nameof(gaps));
        var validatedLessons = TextValidation.RequireEntries(lessons, nameof(lessons));
        var validatedJobAnalysisId = ValidateJobAnalysisId(jobAnalysisId);

        Company = validatedCompany;
        Role = validatedRole;
        InterviewRound = validatedRound;
        SequenceNumber = validatedSequenceNumber;
        OtherLabel = validatedOtherLabel;
        Date = validatedDate;
        Questions = validatedQuestions;
        Gaps = validatedGaps;
        Lessons = validatedLessons;
        JobAnalysisId = validatedJobAnalysisId;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Invariant 19: deleting the referenced JobAnalysis nulls this reference, never deletes the InterviewNote. Called by the (later) DeleteJobAnalysisUseCase, not by anything in this slice.</summary>
    public void ClearJobAnalysisReference(DateTime updatedAtUtc)
    {
        JobAnalysisId = null;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static int ValidateSequenceNumber(int sequenceNumber)
    {
        if (sequenceNumber <= 0)
        {
            throw new DomainValidationException("SequenceNumber must be greater than zero.");
        }

        return sequenceNumber;
    }

    /// <summary>Invariant 18: OtherLabel is required when InterviewRound is Other, and must be null otherwise.</summary>
    private static string? ValidateOtherLabel(InterviewRound interviewRound, string? otherLabel)
    {
        if (interviewRound == InterviewRound.Other)
        {
            return TextValidation.RequireNonBlank(otherLabel ?? string.Empty, nameof(otherLabel), ValidationLimits.ShortTextMaxLength);
        }

        if (!string.IsNullOrWhiteSpace(otherLabel))
        {
            throw new DomainValidationException("otherLabel must be null unless interviewRound is Other.");
        }

        return null;
    }

    private static DateOnly ValidateDate(DateOnly date)
    {
        if (date == default)
        {
            throw new DomainValidationException("date is required.");
        }

        return date;
    }

    private static Guid? ValidateJobAnalysisId(Guid? jobAnalysisId)
    {
        if (jobAnalysisId == Guid.Empty)
        {
            throw new DomainValidationException("jobAnalysisId must not be an empty Guid.");
        }

        return jobAnalysisId;
    }
}
