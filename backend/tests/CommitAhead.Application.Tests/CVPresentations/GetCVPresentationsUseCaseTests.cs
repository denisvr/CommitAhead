using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class GetCVPresentationsUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId, string label) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), label, "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyTheCurrentOwnersPresentations()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        await repository.AddAsync(CreatePresentation(ownerUserId, "Mine"), CancellationToken.None);
        await repository.AddAsync(CreatePresentation(Guid.NewGuid(), "Someone else's"), CancellationToken.None);
        var useCase = new GetCVPresentationsUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var results = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["Mine"], results.Select(r => r.Label));
    }
}
