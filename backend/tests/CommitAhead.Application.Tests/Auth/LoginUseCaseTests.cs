using System.Security.Cryptography;
using System.Text;
using CommitAhead.Application.Auth;

namespace CommitAhead.Application.Tests.Auth;

public class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_InitiatesMagicLink_WithMatchingPkcePair()
    {
        var authClient = new FakeSupabaseAuthClient();
        var useCase = new LoginUseCase(authClient);

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
        var useCase = new LoginUseCase(authClient);

        var first = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);
        var second = await useCase.ExecuteAsync("owner@example.com", CancellationToken.None);

        Assert.NotEqual(first, second);
    }
}
