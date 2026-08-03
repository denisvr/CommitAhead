using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public class DeleteStudyItemUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WhenItemHasNoReviewsOrEvidenceLinks_DeletesIt()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new DeleteStudyItemUseCase(repository, new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(DeleteStudyItemResult.Success, result);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemHasReviews_IsBlocked_AndDoesNotDelete()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        item.AddReview(new StudyReview(Guid.NewGuid(), Now, 4, null), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new DeleteStudyItemUseCase(repository, new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(DeleteStudyItemResult.Blocked, result);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemHasEvidenceLinksButNoReviews_IsBlocked_AndDoesNotDelete()
    {
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository();
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new DeleteStudyItemUseCase(repository, new FakeEvidenceLinkQuery { AnyTargeting = true }, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(DeleteStudyItemResult.Blocked, result);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheDatabaseRejectsTheDelete_ReturnsBlocked()
    {
        // Simulates the race the Restrict FK guards against: a review or evidence link is
        // inserted concurrently after CanBeHardDeleted/AnyTargetingStudyItemAsync both passed.
        var ownerUserId = Guid.NewGuid();
        var item = new StudyItem(Guid.NewGuid(), ownerUserId, "Title", StudyItemCategory.Theory, 2, 2, [], new TheoryDetails("s", [], [], []), Now);
        var repository = new FakeStudyItemRepository { RejectNextDelete = true };
        await repository.AddAsync(item, CancellationToken.None);
        var useCase = new DeleteStudyItemUseCase(repository, new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(item.Id, CancellationToken.None);

        Assert.Equal(DeleteStudyItemResult.Blocked, result);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeStudyItemRepository();
        var useCase = new DeleteStudyItemUseCase(repository, new FakeEvidenceLinkQuery(), new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(DeleteStudyItemResult.NotFound, result);
    }
}
