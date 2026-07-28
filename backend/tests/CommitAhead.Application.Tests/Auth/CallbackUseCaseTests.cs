using CommitAhead.Application.Auth;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.Identity;

namespace CommitAhead.Application.Tests.Auth;

public class CallbackUseCaseTests
{
    private static readonly SupabaseTokenResult Tokens = new(
        "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15), "supabase-sub-123");

    [Fact]
    public async Task ExecuteAsync_WhenUserExistsAndEnabled_IsAllowed()
    {
        var authClient = new FakeSupabaseAuthClient { TokenToReturn = Tokens };
        var userRepository = new FakeUserRepository();
        await userRepository.AddAsync(new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", DateTime.UtcNow), CancellationToken.None);
        var useCase = new CallbackUseCase(authClient, userRepository);

        var result = await useCase.ExecuteAsync("auth-code", "code-verifier", CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.Same(Tokens, result.Tokens);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMatchingUser_IsDenied()
    {
        var authClient = new FakeSupabaseAuthClient { TokenToReturn = Tokens };
        var userRepository = new FakeUserRepository();
        var useCase = new CallbackUseCase(authClient, userRepository);

        var result = await useCase.ExecuteAsync("auth-code", "code-verifier", CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsDisabled_IsDenied()
    {
        var authClient = new FakeSupabaseAuthClient { TokenToReturn = Tokens };
        var userRepository = new FakeUserRepository();
        var user = new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", DateTime.UtcNow);
        user.Disable();
        await userRepository.AddAsync(user, CancellationToken.None);
        var useCase = new CallbackUseCase(authClient, userRepository);

        var result = await useCase.ExecuteAsync("auth-code", "code-verifier", CancellationToken.None);

        Assert.False(result.IsAllowed);
    }
}
