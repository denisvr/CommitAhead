using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.AnalysisDrafts;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.Tests.InterviewNotes;

public class DeleteInterviewNoteUseCaseTests
{
    private static InterviewNote CreateNote(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], null, DateTime.UtcNow);

    private static DeleteInterviewNoteUseCase CreateUseCase(
        FakeInterviewNoteRepository repository, Guid ownerUserId, FakeEvidenceLinkRepository? evidenceLinkRepository = null, FakeAnalysisDraftRepository? analysisDraftRepository = null) =>
        new(
            repository, evidenceLinkRepository ?? new FakeEvidenceLinkRepository(), analysisDraftRepository ?? new FakeAnalysisDraftRepository(), new FakeUnitOfWork(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    [Fact]
    public async Task ExecuteAsync_WithAnExistingNote_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeInterviewNoteRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await repository.AddAsync(note, CancellationToken.None);
        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(note.Id, CancellationToken.None);

        Assert.Equal(InterviewNoteMutationResult.Success, result);
        Assert.Empty(repository.Notes);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingNote_ReturnsNotFound()
    {
        var repository = new FakeInterviewNoteRepository();
        var useCase = CreateUseCase(repository, Guid.NewGuid());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(InterviewNoteMutationResult.NotFound, result);
    }

    /// <summary>ADR-0011: deleting the source must also remove its EvidenceLinks and AnalysisDrafts, leaving unrelated ones untouched.</summary>
    [Fact]
    public async Task ExecuteAsync_DeletesEvidenceLinksAndAnalysisDraftsForThisSource_ButLeavesOthersUntouched()
    {
        var repository = new FakeInterviewNoteRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await repository.AddAsync(note, CancellationToken.None);

        var otherSourceId = Guid.NewGuid();
        var evidenceLinkRepository = new FakeEvidenceLinkRepository();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.InterviewNote, note.Id, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow), CancellationToken.None);
        var otherSourceLink = new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.InterviewNote, otherSourceId, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow);
        await evidenceLinkRepository.AddAsync(otherSourceLink, CancellationToken.None);

        var analysisDraftRepository = new FakeAnalysisDraftRepository();
        await analysisDraftRepository.AddAsync(
            new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.InterviewNote, note.Id, [], [], [], DateTime.UtcNow), CancellationToken.None);
        var otherSourceDraft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.InterviewNote, otherSourceId, [], [], [], DateTime.UtcNow);
        await analysisDraftRepository.AddAsync(otherSourceDraft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId, evidenceLinkRepository, analysisDraftRepository);

        var result = await useCase.ExecuteAsync(note.Id, CancellationToken.None);

        Assert.Equal(InterviewNoteMutationResult.Success, result);
        Assert.Equal([otherSourceLink], evidenceLinkRepository.Links);
        Assert.Equal([otherSourceDraft], analysisDraftRepository.Drafts);
    }
}
