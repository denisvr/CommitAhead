using CommitAhead.Application.Auth;

namespace CommitAhead.Application.Tests.Auth;

public class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_RevokesTheGivenAccessToken()
    {
        var authClient = new FakeSupabaseAuthClient();
        var useCase = new LogoutUseCase(authClient);

        await useCase.ExecuteAsync("access-token", CancellationToken.None);

        Assert.Equal("access-token", authClient.LastRevokedAccessToken);
    }
}
