namespace CommitAhead.Domain.AIUsage;

/// <summary>
/// Every length ceiling referenced by AIUsageRecord domain validation. A local copy, not shared
/// with other aggregates' <c>ValidationLimits</c> — same precedent as JobAnalyses' and StudyItems'
/// own copies.
/// </summary>
public static class ValidationLimits
{
    public const int IdempotencyKeyMaxLength = 200;
    public const int ProviderMaxLength = 100;
    public const int ModelMaxLength = 100;
    public const int PricingVersionMaxLength = 100;

    /// <summary>ISO 4217 currency codes are always exactly 3 letters.</summary>
    public const int CurrencyCodeLength = 3;

    public const int OutcomeCodeMaxLength = 100;
}
