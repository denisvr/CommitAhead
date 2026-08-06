using CommitAhead.Domain;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Domain.Tests.InterviewNotes;

public class InterviewNoteTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = CreatedAt.AddDays(1);
    private static readonly DateOnly ValidDate = new(2026, 1, 15);

    private static InterviewNote CreateNote(
        InterviewRound round = InterviewRound.Technical,
        string? otherLabel = null,
        Guid? jobAnalysisId = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Acme", "Backend Engineer", round, 1, otherLabel, ValidDate, ["Tell me about yourself"], ["Didn't know Postgres well"], ["Study indexing"], jobAnalysisId, CreatedAt);

    [Fact]
    public void Constructor_WithValidArguments_StoresEveryField()
    {
        var note = CreateNote();

        Assert.Equal("Acme", note.Company);
        Assert.Equal("Backend Engineer", note.Role);
        Assert.Equal(InterviewRound.Technical, note.InterviewRound);
        Assert.Equal(1, note.SequenceNumber);
        Assert.Null(note.OtherLabel);
        Assert.Equal(ValidDate, note.Date);
        Assert.Equal(["Tell me about yourself"], note.Questions);
        Assert.Null(note.JobAnalysisId);
        Assert.Equal(CreatedAt, note.CreatedAtUtc);
        Assert.Equal(CreatedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.Empty, Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, 1, null, ValidDate, [], [], [], null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.Empty, "Acme", "Engineer", InterviewRound.Technical, 1, null, ValidDate, [], [], [], null, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithANonPositiveSequenceNumber_Throws(int sequenceNumber)
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, sequenceNumber, null, ValidDate, [], [], [], null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithOtherRoundAndNoLabel_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateNote(InterviewRound.Other, otherLabel: null));
    }

    [Fact]
    public void Constructor_WithOtherRoundAndABlankLabel_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateNote(InterviewRound.Other, otherLabel: "   "));
    }

    [Fact]
    public void Constructor_WithOtherRoundAndALabel_Succeeds()
    {
        var note = CreateNote(InterviewRound.Other, otherLabel: "Take-home follow-up");

        Assert.Equal("Take-home follow-up", note.OtherLabel);
    }

    [Fact]
    public void Constructor_WithANonOtherRoundAndALabel_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateNote(InterviewRound.Technical, otherLabel: "Should not be here"));
    }

    [Fact]
    public void Constructor_WithDefaultDate_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, 1, null, default, [], [], [], null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithAnEmptyJobAnalysisId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateNote(jobAnalysisId: Guid.Empty));
    }

    [Fact]
    public void Constructor_WithANonEmptyJobAnalysisId_Succeeds()
    {
        var jobAnalysisId = Guid.NewGuid();

        var note = CreateNote(jobAnalysisId: jobAnalysisId);

        Assert.Equal(jobAnalysisId, note.JobAnalysisId);
    }

    [Fact]
    public void Constructor_WithNullQuestions_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, 1, null, ValidDate, null!, [], [], null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithNullGaps_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, 1, null, ValidDate, [], null!, [], null, CreatedAt));
    }

    [Fact]
    public void Constructor_WithNullLessons_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new InterviewNote(
            Guid.NewGuid(), Guid.NewGuid(), "Acme", "Engineer", InterviewRound.Technical, 1, null, ValidDate, [], [], null!, null, CreatedAt));
    }

    [Fact]
    public void Update_ReplacesEveryFieldAndBumpsUpdatedAt()
    {
        var note = CreateNote();
        var jobAnalysisId = Guid.NewGuid();

        note.Update("Globex", "Senior Engineer", InterviewRound.Final, 2, null, ValidDate.AddDays(7), ["Q1"], ["G1"], ["L1"], jobAnalysisId, UpdatedAt);

        Assert.Equal("Globex", note.Company);
        Assert.Equal(InterviewRound.Final, note.InterviewRound);
        Assert.Equal(2, note.SequenceNumber);
        Assert.Equal(jobAnalysisId, note.JobAnalysisId);
        Assert.Equal(UpdatedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithANonPositiveSequenceNumber_ThrowsAndLeavesEveryFieldUnchanged()
    {
        var note = CreateNote();

        Assert.Throws<DomainValidationException>(() => note.Update(
            "Globex", "Senior Engineer", InterviewRound.Final, 0, null, ValidDate.AddDays(7), ["Q1"], ["G1"], ["L1"], null, UpdatedAt));

        Assert.Equal("Acme", note.Company);
        Assert.Equal(InterviewRound.Technical, note.InterviewRound);
        Assert.Equal(CreatedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void ClearJobAnalysisReference_SetsJobAnalysisIdToNullAndBumpsUpdatedAt()
    {
        var note = CreateNote(jobAnalysisId: Guid.NewGuid());

        note.ClearJobAnalysisReference(UpdatedAt);

        Assert.Null(note.JobAnalysisId);
        Assert.Equal(UpdatedAt, note.UpdatedAtUtc);
    }
}
