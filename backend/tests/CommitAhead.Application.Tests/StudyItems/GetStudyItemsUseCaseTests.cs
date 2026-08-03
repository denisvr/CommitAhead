using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class GetStudyItemsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static StudyItem CreateItem(Guid ownerUserId, string title) =>
        new(Guid.NewGuid(), ownerUserId, title, StudyItemCategory.Theory, 3, 3, [], new TheoryDetails("s", [], [], []), Now);

    [Fact]
    public async Task ExecuteAsync_WithoutStatusFilter_ReturnsActiveAndArchivedItems()
    {
        var ownerUserId = Guid.NewGuid();
        var active = CreateItem(ownerUserId, "Active item");
        var archived = CreateItem(ownerUserId, "Archived item");
        archived.Archive(Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(archived, CancellationToken.None);
        var useCase = new GetStudyItemsUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync(status: null, CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveStatusFilter_ExcludesArchivedItems()
    {
        var ownerUserId = Guid.NewGuid();
        var active = CreateItem(ownerUserId, "Active item");
        var archived = CreateItem(ownerUserId, "Archived item");
        archived.Archive(Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(archived, CancellationToken.None);
        var useCase = new GetStudyItemsUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync("Active", CancellationToken.None);

        Assert.Equal(["Active item"], results.Select(r => r.Title));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUndefinedStatusString_Throws()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new GetStudyItemsUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("not-a-status", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ScopedToADifferentOwner_ReturnsEmpty()
    {
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(CreateItem(Guid.NewGuid(), "Someone else's item"), CancellationToken.None);
        var useCase = new GetStudyItemsUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync(status: null, CancellationToken.None);

        Assert.Empty(results);
    }
}
