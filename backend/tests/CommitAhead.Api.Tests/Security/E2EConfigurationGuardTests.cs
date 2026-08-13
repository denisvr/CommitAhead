using CommitAhead.Api.Security;
using CommitAhead.Infrastructure.AI;
using Microsoft.Extensions.Configuration;

namespace CommitAhead.Api.Tests.Security;

public sealed class E2EConfigurationGuardTests
{
    private static readonly Dictionary<string, string?> ValidE2ESentinelSettings = new()
    {
        ["E2E:SigningKey"] = "e2e-test-signing-key-at-least-32-bytes-long",
        ["E2E:Issuer"] = "https://e2e.commitahead.local/auth/v1",
        ["E2E:SupabaseUserId"] = "e2e-user",
        ["Supabase:Url"] = E2EConfigurationGuard.SupabaseUrlSentinel,
        ["Supabase:AnonKey"] = E2EConfigurationGuard.SupabaseAnonKeySentinel,
        ["Auth:CallbackUrl"] = E2EConfigurationGuard.AuthCallbackUrlSentinel,
        ["AI:Providers:Anthropic:BaseUrl"] = AnthropicBaseAddress.E2ESentinel,
        ["AI:Providers:Anthropic:ApiKey"] = E2EConfigurationGuard.AnthropicApiKeySentinel,
    };

    [Theory]
    [InlineData("Development")]
    [InlineData("Docker")]
    [InlineData("Production")]
    public void Validate_OutsideE2E_WithNoE2EConfiguration_DoesNotThrow(string environmentName)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        E2EConfigurationGuard.Validate(configuration, environmentName);
    }

    [Theory]
    [InlineData("Development", "E2E:SigningKey")]
    [InlineData("Docker", "E2E:Issuer")]
    [InlineData("Production", "E2E:SupabaseUserId")]
    public void Validate_OutsideE2E_WithAnyE2EConfigurationPresent_Throws(string environmentName, string presentKey)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [presentKey] = "should-not-be-here" });

        Assert.Throws<InvalidOperationException>(() => E2EConfigurationGuard.Validate(configuration, environmentName));
    }

    [Fact]
    public void Validate_InsideE2E_WithAllExactSentinels_DoesNotThrow()
    {
        var configuration = BuildConfiguration(ValidE2ESentinelSettings);

        E2EConfigurationGuard.Validate(configuration, "E2E");
    }

    [Theory]
    [InlineData("E2E:SigningKey")]
    [InlineData("E2E:Issuer")]
    [InlineData("E2E:SupabaseUserId")]
    public void Validate_InsideE2E_WithARequiredE2EValueMissing_Throws(string missingKey)
    {
        var settings = new Dictionary<string, string?>(ValidE2ESentinelSettings) { [missingKey] = null };
        var configuration = BuildConfiguration(settings);

        Assert.Throws<InvalidOperationException>(() => E2EConfigurationGuard.Validate(configuration, "E2E"));
    }

    [Theory]
    [InlineData("Supabase:Url")]
    [InlineData("Supabase:AnonKey")]
    [InlineData("Auth:CallbackUrl")]
    [InlineData("AI:Providers:Anthropic:BaseUrl")]
    [InlineData("AI:Providers:Anthropic:ApiKey")]
    public void Validate_InsideE2E_WithASentinelValueThatDoesNotExactlyMatch_Throws(string sentinelKey)
    {
        var settings = new Dictionary<string, string?>(ValidE2ESentinelSettings) { [sentinelKey] = "https://a-real-looking-host.example.com/" };
        var configuration = BuildConfiguration(settings);

        Assert.Throws<InvalidOperationException>(() => E2EConfigurationGuard.Validate(configuration, "E2E"));
    }

    [Fact]
    public void Validate_InsideE2E_RejectsARealAnthropicKeyEvenThoughItDoesNotMatchAnyPrefixHeuristic()
    {
        // The guard checks exact equality against the one approved sentinel, not a prefix like
        // "sk-ant-" — any value other than the sentinel is rejected, real-looking or not.
        var settings = new Dictionary<string, string?>(ValidE2ESentinelSettings)
        {
            ["AI:Providers:Anthropic:ApiKey"] = "sk-ant-totally-real-looking-key-0000000000000000",
        };
        var configuration = BuildConfiguration(settings);

        Assert.Throws<InvalidOperationException>(() => E2EConfigurationGuard.Validate(configuration, "E2E"));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
