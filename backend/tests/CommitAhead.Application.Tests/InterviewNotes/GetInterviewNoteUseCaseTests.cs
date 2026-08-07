using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.Tests.InterviewNotes;

public class GetInterviewNoteUseCaseTests
{
    private static InterviewNote CreateNote(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingNote_ReturnsItsProjection()
    {
        var repository = new FakeInterviewNoteRepository();
        var ownerUserId = Guid.NewGuid();
        var note = CreateNote(ownerUserId);
        await repository.AddAsync(note, CancellationToken.None);
        var useCase = new GetInterviewNoteUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(note.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(note.Id, result.Id);
        Assert.Equal("Acme Corp", result.Company);
    }

    [Fact]
    public async Task ExecuteAsync_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new FakeInterviewNoteRepository();
        var note = CreateNote(Guid.NewGuid());
        await repository.AddAsync(note, CancellationToken.None);
        var useCase = new GetInterviewNoteUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(note.Id, CancellationToken.None);

        Assert.Null(result);
    }
}
