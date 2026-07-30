using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class ArchiveStudyItemUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WhenItemExists_ArchivesIt()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new ArchiveStudyItemUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.Success, result);
        Assert.Equal(StudyItemStatus.Archived, item.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new ArchiveStudyItemUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.NotFound, result);
    }
}
