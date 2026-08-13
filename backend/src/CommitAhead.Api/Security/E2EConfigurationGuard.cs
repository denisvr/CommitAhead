using CommitAhead.Infrastructure.AI;

namespace CommitAhead.Api.Security;

/// <summary>
/// Fail-closed startup validation for the E2E environment (E2E Foundation Plan). Runs
/// unconditionally from Program.cs on every startup, including build-time OpenAPI document
/// generation — safe because that host never runs under ASPNETCORE_ENVIRONMENT=E2E, so the
/// "must be present inside E2E" branch never fires there, and the "must be absent outside E2E"
/// branch only rejects a misconfiguration that should never exist anyway. Every sentinel is
/// checked by exact string equality, never a prefix or "looks safe" heuristic — a real provider
/// URL or credential is rejected because it differs from the one approved value, not because it
/// fails some pattern match. Exception messages name only the configuration key and its expected
/// E2E category — never the configured value or the sentinel itself — so a real secret pasted
/// into <c>AI:Providers:Anthropic:ApiKey</c> by mistake never reaches startup logs or test output.
/// </summary>
public static class E2EConfigurationGuard
{
    public const string SupabaseUrlSentinel = "http://external-stub:8080/";
    public const string SupabaseAnonKeySentinel = "e2e-anon-key";
    public const string AuthCallbackUrlSentinel = "http://localhost:8081/auth/callback";
    public const string AnthropicApiKeySentinel = "e2e-stub-key";

    private const string E2EEnvironmentName = "E2E";

    public static void Validate(IConfiguration configuration, string environmentName)
    {
        var isE2E = string.Equals(environmentName, E2EEnvironmentName, StringComparison.Ordinal);

        var signingKey = configuration[$"{E2EOptions.SectionName}:SigningKey"];
        var issuer = configuration[$"{E2EOptions.SectionName}:Issuer"];
        var supabaseUserId = configuration[$"{E2EOptions.SectionName}:SupabaseUserId"];
        var anyE2EConfigPresent = !string.IsNullOrWhiteSpace(signingKey)
            || !string.IsNullOrWhiteSpace(issuer)
            || !string.IsNullOrWhiteSpace(supabaseUserId);

        if (!isE2E)
        {
            if (anyE2EConfigPresent)
            {
                throw new InvalidOperationException(
                    $"E2E:* configuration must not be present outside the E2E environment. ASPNETCORE_ENVIRONMENT is '{environmentName}'.");
            }

            RejectSentinelOutsideE2E(configuration, environmentName, "Supabase:Url", "Supabase URL", SupabaseUrlSentinel);
            RejectSentinelOutsideE2E(configuration, environmentName, "Supabase:AnonKey", "Supabase anonymous key", SupabaseAnonKeySentinel);
            RejectSentinelOutsideE2E(configuration, environmentName, "Auth:CallbackUrl", "auth callback URL", AuthCallbackUrlSentinel);
            RejectSentinelOutsideE2E(configuration, environmentName, $"{AnthropicOptions.SectionName}:BaseUrl", "Anthropic base URL", AnthropicBaseAddress.E2ESentinel);
            RejectSentinelOutsideE2E(configuration, environmentName, $"{AnthropicOptions.SectionName}:ApiKey", "Anthropic API key", AnthropicApiKeySentinel);

            return;
        }

        var missing = new List<string>(3);
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            missing.Add($"{E2EOptions.SectionName}:SigningKey");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            missing.Add($"{E2EOptions.SectionName}:Issuer");
        }

        if (string.IsNullOrWhiteSpace(supabaseUserId))
        {
            missing.Add($"{E2EOptions.SectionName}:SupabaseUserId");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Missing required E2E configuration: {string.Join(", ", missing)}.");
        }

        RequireExactSentinel(configuration, "Supabase:Url", "Supabase URL", SupabaseUrlSentinel);
        RequireExactSentinel(configuration, "Supabase:AnonKey", "Supabase anonymous key", SupabaseAnonKeySentinel);
        RequireExactSentinel(configuration, "Auth:CallbackUrl", "auth callback URL", AuthCallbackUrlSentinel);
        RequireExactSentinel(configuration, $"{AnthropicOptions.SectionName}:BaseUrl", "Anthropic base URL", AnthropicBaseAddress.E2ESentinel);
        RequireExactSentinel(configuration, $"{AnthropicOptions.SectionName}:ApiKey", "Anthropic API key", AnthropicApiKeySentinel);
    }

    private static void RequireExactSentinel(IConfiguration configuration, string key, string category, string expectedValue)
    {
        var actual = configuration[key];
        if (!string.Equals(actual, expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{key} must equal the approved E2E {category} sentinel when ASPNETCORE_ENVIRONMENT=E2E.");
        }
    }

    private static void RejectSentinelOutsideE2E(IConfiguration configuration, string environmentName, string key, string category, string sentinelValue)
    {
        var actual = configuration[key];
        if (string.Equals(actual, sentinelValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{key} must not equal the E2E {category} sentinel outside the E2E environment. ASPNETCORE_ENVIRONMENT is '{environmentName}'.");
        }
    }
}
