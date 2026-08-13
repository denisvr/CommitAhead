namespace CommitAhead.Infrastructure.AI;

/// <summary>
/// Bound from "AI:Providers:Anthropic" (ApiKey via user-secrets/environment only, never
/// committed; Model has a checked-in appsettings.json default — it selects among
/// AnthropicModelProfiles.All, never accepted as free-form pricing-affecting input).
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "AI:Providers:Anthropic";

    public required string ApiKey { get; set; }

    public required string Model { get; set; }

    /// <summary>
    /// Optional override for the Anthropic API base address. Left unset outside the E2E
    /// environment, this defaults to <see cref="AnthropicBaseAddress.ProductionDefault"/>. See
    /// <see cref="AnthropicBaseAddress.Resolve"/> for the validation this value is subject to —
    /// this override exists so the E2E stack can redirect the real provider to a local
    /// deterministic stub, never so a real deployment can point at an untrusted host.
    /// </summary>
    public string? BaseUrl { get; set; }
}
