using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class StudyReviewTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidArguments_CreatesReview()
    {
        var review = new StudyReview(Guid.NewGuid(), Now, 4, "Went well");

        Assert.Equal(4, review.ConfidenceRating);
        Assert.Equal("Went well", review.NotesMarkdown);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new StudyReview(Guid.Empty, Now, 3, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_WithConfidenceRatingOutOfRange_Throws(int confidenceRating)
    {
        Assert.Throws<DomainValidationException>(() => new StudyReview(Guid.NewGuid(), Now, confidenceRating, null));
    }

    [Fact]
    public void Constructor_WithoutNotes_AllowsNull()
    {
        var review = new StudyReview(Guid.NewGuid(), Now, 3, null);

        Assert.Null(review.NotesMarkdown);
    }

    [Fact]
    public void Constructor_TrimsNotes()
    {
        var review = new StudyReview(Guid.NewGuid(), Now, 3, "  Went well  ");

        Assert.Equal("Went well", review.NotesMarkdown);
    }

    [Fact]
    public void Constructor_WithBlankNotes_TreatsAsNull()
    {
        var review = new StudyReview(Guid.NewGuid(), Now, 3, "   ");

        Assert.Null(review.NotesMarkdown);
    }

    [Fact]
    public void Constructor_WithNotesLongerThanMaxLength_Throws()
    {
        var notes = new string('a', ValidationLimits.MarkdownMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => new StudyReview(Guid.NewGuid(), Now, 3, notes));
    }
}
