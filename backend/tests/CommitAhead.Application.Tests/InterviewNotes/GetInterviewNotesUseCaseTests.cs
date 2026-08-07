using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.Tests.InterviewNotes;

public class GetInterviewNotesUseCaseTests
{
    private static InterviewNote CreateNote(Guid ownerUserId, string company) => new(
        Guid.NewGuid(), ownerUserId, company, "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyTheCurrentOwnersNotes()
    {
        var repository = new FakeInterviewNoteRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(CreateNote(ownerUserId, "Mine"), CancellationToken.None);
        await repository.AddAsync(CreateNote(Guid.NewGuid(), "Someone else's"), CancellationToken.None);
        var useCase = new GetInterviewNotesUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["Mine"], results.Select(r => r.Company));
    }
}
