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
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("access-token", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRevokeTimesOut_TreatsItLikeAnyOtherProviderFailure_AndDoesNotPropagate()
    {
        // HttpClient's own configured timeout throws via the OperationCanceledException
        // hierarchy (TaskCanceledException) — this must be swallowed and logged just like an
        // HttpRequestException, not treated as if the caller cancelled the request.
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnRevoke = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."),
        };
        var logger = new RecordingLogger<LogoutUseCase>();
        var useCase = new LogoutUseCase(authClient, logger);

        // The caller's own token is never cancelled — only the provider's internal timeout fired.
        var exception = await Record.ExceptionAsync(() => useCase.ExecuteAsync("access-token", CancellationToken.None));

        Assert.Null(exception);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerCancellationIsRequested_PropagatesTheCancellation()
    {
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnRevoke = new OperationCanceledException("Caller aborted the request."),
        };
        var useCase = new LogoutUseCase(authClient, NullLogger<LogoutUseCase>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => useCase.ExecuteAsync("access-token", cts.Token));
    }
}
