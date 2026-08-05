using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class CreateStudyItemUseCaseTests
{
    private static TheoryDetails ValidTheoryDetails() => new("Summary", [], [], []);

    [Fact]
    public async Task ExecuteAsync_AddsAnActiveItem_OwnedByTheCurrentUser()
    {
        var repository = new FakeStudyItemRepository();
        var ownerUserId = Guid.NewGuid();
        var useCase = new CreateStudyItemUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var id = await useCase.ExecuteAsync(
            "CAP theorem trade-offs", StudyItemCategory.Theory, 3, 2, ["distributed-systems"], ValidTheoryDetails(), CancellationToken.None);

        var created = Assert.Single(repository.Items);
        Assert.Equal(id, created.Id);
        Assert.Equal(ownerUserId, created.OwnerUserId);
        Assert.Equal(StudyItemStatus.Active, created.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidImportance_PropagatesTheDomainValidationException()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new CreateStudyItemUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            "Title", StudyItemCategory.Theory, importance: 0, initialMastery: 2, [], ValidTheoryDetails(), CancellationToken.None));
        Assert.Empty(repository.Items);
    }
}
