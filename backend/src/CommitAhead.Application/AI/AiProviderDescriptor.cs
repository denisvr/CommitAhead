namespace CommitAhead.Application.AI;

/// <summary>
/// A provider implementation's own execution metadata for one <see cref="CommitAhead.Domain.AIUsage.AiCommandType"/>
/// (docs/tbd.md — provider/model selection). The analyzing use case reads this before calling the
/// provider at all, so it never hardcodes provider/model/pricing/limits itself: <c>AiCallLimits</c>
/// is built entirely from <see cref="MaxInputTokens"/>/<see cref="MaxOutputTokens"/>/<see cref="Timeout"/>,
/// and the pre-call <c>AIUsageRecord</c> reservation is built from every other field here.
/// </summary>
public sealed record AiProviderDescriptor(
    string Provider,
    string Model,
    string PricingVersion,
    string Currency,
    int MaxInputTokens,
    int MaxOutputTokens,
    TimeSpan Timeout,
    decimal EstimatedMaxCost);
