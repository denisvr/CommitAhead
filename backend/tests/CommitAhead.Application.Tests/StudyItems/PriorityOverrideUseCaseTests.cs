using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class PriorityOverrideUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static (FakeStudyItemRepository Repository, StudyItem Item, Guid OwnerUserId) SeedItem()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        repository.AddAsync(item, CancellationToken.None).GetAwaiter().GetResult();
        return (repository, item, ownerUserId);
    }

    [Fact]
    public async Task Set_WhenItemExists_SetsTheOverride()
    {
        var (repository, item, ownerUserId) = SeedItem();
        var useCase = new SetPriorityOverrideUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, 90, "Interview next week", CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.Success, result);
        Assert.NotNull(item.PriorityOverride);
        Assert.Equal(90, item.PriorityOverride!.Score);
    }

    [Fact]
    public async Task Set_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new SetPriorityOverrideUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), 50, "reason", CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.NotFound, result);
    }

    [Fact]
    public async Task Clear_WhenOverrideIsSet_RemovesIt()
    {
        var (repository, item, ownerUserId) = SeedItem();
        item.SetPriorityOverride(new Domain.StudyItems.PriorityOverride(90, "reason"), Now);
        var useCase = new ClearPriorityOverrideUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.Success, result);
        Assert.Null(item.PriorityOverride);
    }

    [Fact]
    public async Task Clear_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new ClearPriorityOverrideUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StudyItemMutationResult.NotFound, result);
    }
}
