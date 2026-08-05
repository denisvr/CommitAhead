using CommitAhead.Domain;

namespace CommitAhead.Domain.StudyItems;

public sealed class StudyReview
{
    public Guid Id { get; }
    public DateTime ReviewedAtUtc { get; }
    public int ConfidenceRating { get; }
    public string? NotesMarkdown { get; }

    public StudyReview(Guid id, DateTime reviewedAtUtc, int confidenceRating, string? notesMarkdown)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (confidenceRating is < 1 or > 5)
        {
            throw new DomainValidationException("ConfidenceRating must be in [1,5].");
        }

        Id = id;
        ReviewedAtUtc = reviewedAtUtc;
        ConfidenceRating = confidenceRating;
        NotesMarkdown = notesMarkdown;
    }
}
