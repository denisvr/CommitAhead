using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class GetCVPresentationUseCaseTests
{
    private static CVPresentation CreatePresentation(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, Guid.NewGuid(), "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingPresentation_ReturnsItsProjection()
    {
        var repository = new FakeCVPresentationRepository();
        var ownerUserId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId);
        await repository.AddAsync(presentation, CancellationToken.None);
        var useCase = new GetCVPresentationUseCase(repository, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(presentation.Id, result.Id);
        Assert.Equal("Label", result.Label);
    }

    [Fact]
    public async Task ExecuteAsync_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(Guid.NewGuid());
        await repository.AddAsync(presentation, CancellationToken.None);
        var useCase = new GetCVPresentationUseCase(repository, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" });

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Null(result);
    }
}
