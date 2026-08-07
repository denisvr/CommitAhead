using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Domain;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.InterviewNotes;

public class UpdateInterviewNoteUseCaseTests
{
    private static InterviewNote CreateNote(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingNote_UpdatesItAndReturnsSuccess()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var useCase = new UpdateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(
            note.Id, "New Corp", "Senior Backend Engineer", InterviewRound.Behavioral, 2, null, new DateOnly(2026, 2, 1),
            ["Q2"], ["Gap2"], ["Lesson2"], null, CancellationToken.None);

        Assert.Equal(InterviewNoteMutationResult.Success, result);
        Assert.Equal("New Corp", note.Company);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingNote_ReturnsNotFound()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var useCase = new UpdateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
            ["Q1"], ["Gap1"], ["Lesson1"], null, CancellationToken.None);

        Assert.Equal(InterviewNoteMutationResult.NotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithAJobAnalysisIdThatDoesNotExist_ThrowsDomainValidationExceptionAndLeavesTheNoteUnchanged()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var useCase = new UpdateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            note.Id, "New Corp", "Senior Backend Engineer", InterviewRound.Behavioral, 2, null, new DateOnly(2026, 2, 1),
            ["Q2"], ["Gap2"], ["Lesson2"], Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("Acme Corp", note.Company);
    }

    [Fact]
    public async Task ExecuteAsync_WithAJobAnalysisIdOwnedByAnotherUser_ThrowsDomainValidationException()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var otherOwnersAnalysis = new JobAnalysis(Guid.NewGuid(), Guid.NewGuid(), "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(otherOwnersAnalysis, CancellationToken.None);
        var useCase = new UpdateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            note.Id, "New Corp", "Senior Backend Engineer", InterviewRound.Behavioral, 2, null, new DateOnly(2026, 2, 1),
            ["Q2"], ["Gap2"], ["Lesson2"], otherOwnersAnalysis.Id, CancellationToken.None));
    }
}
