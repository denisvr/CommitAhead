namespace CommitAhead.Infrastructure.AI;

/// <summary>
/// Resolves and validates <see cref="AnthropicOptions.BaseUrl"/>. Kept as a pure function of two
/// primitives (never <c>IWebHostEnvironment</c>) so Infrastructure gains no new dependency and the
/// rules are testable without a host. Outside the E2E environment the address must be an absolute
/// HTTPS URI (defaulting to the real Anthropic API); inside E2E it must equal the internal stub
/// sentinel exactly — never merely "looks internal" or "isn't the real host". See
/// docs/testing/strategy.md §7.6 for why E2E redirects the real provider rather than replacing it
/// with a fake, and the E2E Foundation Plan for why the sentinel is checked by exact match.
/// </summary>
public static class AnthropicBaseAddress
{
    public const string ProductionDefault = "https://api.anthropic.com/";

    public const string E2ESentinel = "http://external-stub:8080/";

    private const string E2EEnvironmentName = "E2E";

    public static Uri Resolve(string? configuredValue, string environmentName)
    {
        var isE2E = string.Equals(environmentName, E2EEnvironmentName, StringComparison.Ordinal);
        var value = string.IsNullOrWhiteSpace(configuredValue) ? ProductionDefault : configuredValue;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"{AnthropicOptions.SectionName}:BaseUrl must be an absolute URI. Configured value: '{value}'.");
        }

        if (isE2E)
        {
            if (!string.Equals(value, E2ESentinel, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{AnthropicOptions.SectionName}:BaseUrl must equal the E2E stub sentinel '{E2ESentinel}' when ASPNETCORE_ENVIRONMENT=E2E. Configured value: '{value}'.");
            }
        }
        else if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{AnthropicOptions.SectionName}:BaseUrl must use https outside the E2E environment. Configured value: '{value}'.");
        }

        return uri;
    }
}
