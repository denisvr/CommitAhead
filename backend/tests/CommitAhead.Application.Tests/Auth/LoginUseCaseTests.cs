using System.Security.Cryptography;
using System.Text;
using CommitAhead.Application.Auth;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.Auth;

public class LoginUseCaseTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WithProvisionedEnabledEmail_InitiatesMagicLink_WithMatchingPkcePair()
    {
        var authClient = new FakeSupabaseAuthClient();
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "sub-1", "owner@example.com", Now), CancellationToken.None);
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        var codeVerifier = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);

        Assert.Equal("owner@example.com", authClient.LastEmail);
        Assert.NotNull(authClient.LastCodeChallenge);

        var expectedChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        Assert.Equal(expectedChallenge, authClient.LastCodeChallenge);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesADifferentCodeVerifier_EachCall()
    {
        var authClient = new FakeSupabaseAuthClient();
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "sub-1", "owner@example.com", Now), CancellationToken.None);
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        var first = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);
        var second = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownEmail_DoesNotCallSupabase_ButStillReturnsACodeVerifier()
    {
        var authClient = new FakeSupabaseAuthClient();
        var userRepository = new FakeUserRepository();
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        var codeVerifier = await useCase.ExecuteAsync("unknown@example.com", CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(codeVerifier));
        Assert.Null(authClient.LastEmail);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledUserEmail_DoesNotCallSupabase()
    {
        var authClient = new FakeSupabaseAuthClient();
        var userRepository = new FakeUserRepository();
        var user = new User(Guid.NewGuid(), "sub-1", "disabled@example.com", Now);
        user.Disable();
        await userRepository.AddAsync(user, CancellationToken.None);
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        await useCase.ExecuteAsync("disabled@example.com", CancellationToken.None);

        Assert.Null(authClient.LastEmail);
    }

    [Fact]
    public async Task ExecuteAsync_NormalizesEmail_BeforeLookup()
    {
        var authClient = new FakeSupabaseAuthClient();
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "sub-1", "owner@example.com", Now), CancellationToken.None);
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        await useCase.ExecuteAsync("  Owner@Example.com  ", CancellationToken.None);

        Assert.Equal("owner@example.com", authClient.LastEmail);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSupabaseCallFails_StillReturnsACodeVerifier_AndLogsWithoutTheEmail()
    {
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnInitiateMagicLink = new HttpRequestException("Supabase is unreachable"),
        };
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "owner-sub", "owner@example.com", Now), CancellationToken.None);
        var logger = new RecordingLogger<LoginUseCase>();
        var useCase = new LoginUseCase(authClient, userRepository, logger);

        var codeVerifier = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(codeVerifier));
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.DoesNotContain("owner@example.com", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithProvisionedEmailWhereSupabaseFails_ReturnsACodeVerifier_JustLikeAnUnknownEmailWould()
    {
        // The whole point of ADR-0015's closed-login/no-enumeration guarantee: a provisioned
        // user whose Supabase call happens to fail must behave identically (from the
        // controller's perspective — same codeVerifier contract) to an unknown email, not throw.
        var authClient = new FakeSupabaseAuthClient
        {
            ExceptionToThrowOnInitiateMagicLink = new HttpRequestException("Supabase 500"),
        };
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "owner-sub", "owner@example.com", Now), CancellationToken.None);
        var useCase = new LoginUseCase(authClient, userRepository, NullLogger<LoginUseCase>.Instance);

        var exception = await Record.ExceptionAsync(() => useCase.ExecuteAsync("owner@example.com", CancellationToken.None));

        Assert.Null(exception);
    }
}
