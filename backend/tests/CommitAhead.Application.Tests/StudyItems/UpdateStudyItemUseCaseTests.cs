using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class UpdateStudyItemUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static TheoryDetails ValidTheoryDetails() => new("Summary", [], [], []);

    [Fact]
    public async Task ExecuteAsync_WhenItemExistsForTheOwner_UpdatesItAndReturnsSuccess()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Old title", StudyItemCategory.Theory, 2, 2, [], ValidTheoryDetails(), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new UpdateStudyItemUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, "New title", 4, ["tag"], ValidTheoryDetails(), CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.Success, result);
        Assert.Equal("New title", item.Title);
        Assert.Equal(4, item.Importance);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new UpdateStudyItemUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "Title", 3, [], ValidTheoryDetails(), CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.NotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemBelongsToAnotherUser_ReturnsNotFound()
    {
        var item = new StudyItem(Guid.NewGuid(), Guid.NewGuid(), "Title", StudyItemCategory.Theory, 2, 2, [], ValidTheoryDetails(), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new UpdateStudyItemUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "someone-else@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, "New title", 3, [], ValidTheoryDetails(), CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.NotFound, result);
        Assert.Equal("Title", item.Title);
    }
}
