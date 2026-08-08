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
}
