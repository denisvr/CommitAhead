using CommitAhead.Application.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.Auth;

public class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_RevokesTheGivenAccessToken()
    {
        var authClient = new FakeSupabaseAuthClient();
        var useCase = new LogoutUseCase(authClient, NullLogger<LogoutUseCase>.Instance);

        await useCase.ExecuteAsync("access-token", CancellationToken.None);

        Assert.Equal("access-token", authClient.LastRevokedAccessToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRevokeFails_DoesNotThrow_AndLogsASafeError()
    {
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnRevoke = new HttpRequestException("Supabase is unreachable"),
        };
        var logger = new RecordingLogger<LogoutUseCase>();
        var useCase = new LogoutUseCase(authClient, logger);

        var exception = await Record.ExceptionAsync(() => useCase.ExecuteAsync("access-token", CancellationToken.None));

        Assert.Null(exception);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.DoesNotContain("access-token", entry.Message, StringComparison.OrdinalIgnoreCase);
    }
}
