using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Domain;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.InterviewNotes;

public class CreateInterviewNoteUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutAJobAnalysis_CreatesANoteOwnedByTheCurrentUser()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var useCase = new CreateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(
            "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
            ["Q1"], ["Gap1"], ["Lesson1"], null, CancellationToken.None);

        var created = Assert.Single(noteRepository.Notes);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
    }

    [Fact]
    public async Task ExecuteAsync_WithTheCurrentUsersOwnJobAnalysis_CreatesANoteReferencingIt()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var ownerUserId = Guid.NewGuid();
        var jobAnalysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var useCase = new CreateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        await useCase.ExecuteAsync(
            "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
            ["Q1"], ["Gap1"], ["Lesson1"], jobAnalysis.Id, CancellationToken.None);

        var created = Assert.Single(noteRepository.Notes);
        Assert.Equal(jobAnalysis.Id, created.JobAnalysisId);
    }

    [Fact]
    public async Task ExecuteAsync_WithAJobAnalysisIdThatDoesNotExist_ThrowsDomainValidationException()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var useCase = new CreateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
            ["Q1"], ["Gap1"], ["Lesson1"], Guid.NewGuid(), CancellationToken.None));

        Assert.Empty(noteRepository.Notes);
    }

    [Fact]
    public async Task ExecuteAsync_WithAJobAnalysisIdOwnedByAnotherUser_ThrowsDomainValidationException()
    {
        var noteRepository = new FakeInterviewNoteRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var otherOwnersAnalysis = new JobAnalysis(Guid.NewGuid(), Guid.NewGuid(), "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(otherOwnersAnalysis, CancellationToken.None);
        var useCase = new CreateInterviewNoteUseCase(noteRepository, jobAnalysisRepository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
            ["Q1"], ["Gap1"], ["Lesson1"], otherOwnersAnalysis.Id, CancellationToken.None));

        Assert.Empty(noteRepository.Notes);
    }
}
