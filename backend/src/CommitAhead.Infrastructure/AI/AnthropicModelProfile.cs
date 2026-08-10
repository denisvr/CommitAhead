namespace CommitAhead.Infrastructure.AI;

/// <summary>
/// One supported Anthropic model's exact id, pricing, and limits (ADR-0019) — a model can never be
/// selected independently of its own pricing. <see cref="AnthropicOptions.Model"/> must resolve to
/// an entry here; an unrecognized model id fails startup safely (AnthropicAIProvider's constructor),
/// never falls back to another entry's prices.
/// </summary>
public sealed record AnthropicModelProfile(
    string ModelId,
    decimal InputPricePerMillionTokensUsd,
    decimal OutputPricePerMillionTokensUsd,
    string PricingVersion,
    int MaxInputTokens,
    int MaxOutputTokens,
    TimeSpan Timeout);

public static class AnthropicModelProfiles
{
    /// <summary>
    /// Adding a new model (e.g. Claude Sonnet) requires an explicit new entry here with its own
    /// prices — never inferred or copied from an existing one (ADR-0019).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, AnthropicModelProfile> All = new Dictionary<string, AnthropicModelProfile>
    {
        ["claude-haiku-4-5-20251001"] = new AnthropicModelProfile(
            ModelId: "claude-haiku-4-5-20251001",
            InputPricePerMillionTokensUsd: 1.00m,
            OutputPricePerMillionTokensUsd: 5.00m,
            PricingVersion: "anthropic-haiku-4.5-2025-10",
            MaxInputTokens: 8_000,
            MaxOutputTokens: 2_000,
            // A first call using a new/changed Structured Outputs schema can incur one-time schema
            // compilation on Anthropic's side, on top of ordinary generation latency — 195s covers
            // that documented first-call path, not just steady-state calls.
            Timeout: TimeSpan.FromSeconds(195)),
    };

    public static AnthropicModelProfile Resolve(string modelId)
    {
        if (!All.TryGetValue(modelId, out var profile))
        {
            throw new InvalidOperationException(
                $"Unsupported AI:Providers:Anthropic:Model value '{modelId}'. Add an explicit AnthropicModelProfile entry (with its own prices) before configuring it.");
        }

        return profile;
    }
}
