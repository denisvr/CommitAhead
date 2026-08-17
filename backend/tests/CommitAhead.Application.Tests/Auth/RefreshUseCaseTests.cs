using CommitAhead.Application.Auth;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.Auth;

public class RefreshUseCaseTests
{
    private static readonly SupabaseTokenResult Tokens = new(
        "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15), "supabase-sub-123");

    [Fact]
    public async Task ExecuteAsync_WhenUserExistsAndEnabled_IsAllowed()
    {
        var authClient = new FakeSupabaseAuthClient { TokenToReturn = Tokens };
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", DateTime.UtcNow), CancellationToken.None);
        var useCase = new RefreshUseCase(authClient, userRepository, NullLogger<RefreshUseCase>.Instance);

        var result = await useCase.ExecuteAsync("refresh-token", CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserWasDisabledSinceLogin_IsDenied()
    {
        var authClient = new FakeSupabaseAuthClient { TokenToReturn = Tokens };
        var userRepository = new FakeUserRepository();
        var user = new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", DateTime.UtcNow);
        user.Disable();
        await userRepository.AddAsync(user, CancellationToken.None);
        var useCase = new RefreshUseCase(authClient, userRepository, NullLogger<RefreshUseCase>.Instance);

        var result = await useCase.ExecuteAsync("refresh-token", CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    // Reproduces the real bug found while exercising the fully-containerized dev environment
    // (ADR-0022) with a genuinely-unconfigured Supabase:Url: HttpClient itself throws
    // InvalidOperationException on the first real request once BaseAddress is unset, and this
    // must degrade to an ordinary Denied() outcome — never an unhandled exception surfacing as a
    // raw 500 from what should be a graceful 401/403.
    [Fact]
    public async Task ExecuteAsync_WhenSupabaseCallThrows_IsDeniedInsteadOfPropagating()
    {
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnRefresh = new InvalidOperationException(
                "An invalid request URI was provided. The request URI must either be an absolute URI or BaseAddress must be set."),
        };
        var userRepository = new FakeUserRepository();
        var useCase = new RefreshUseCase(authClient, userRepository, NullLogger<RefreshUseCase>.Instance);

        var result = await useCase.ExecuteAsync("refresh-token", CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Null(result.Tokens);
    }
}
