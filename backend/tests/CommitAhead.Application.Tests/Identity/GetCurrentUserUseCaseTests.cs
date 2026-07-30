using CommitAhead.Application.Identity;

namespace CommitAhead.Application.Tests.Identity;

public class GetCurrentUserUseCaseTests
{
    [Fact]
    public void Execute_ReturnsTheCurrentUsersIdAndEmail()
    {
        var userId = Guid.NewGuid();
        var currentUser = new StubCurrentUser { UserId = userId, Email = "owner@example.com" };
        var useCase = new GetCurrentUserUseCase(currentUser);

        var result = useCase.Execute();

        Assert.Equal(userId, result.UserId);
        Assert.Equal("owner@example.com", result.Email);
    }
}
