namespace CommitAhead.Application.AI;

/// <summary>
/// AiBudgetLimits (ADR-0019) is USD-only by construction — thrown before any reservation write or
/// provider call if an IAIProvider ever describes itself with a non-USD currency, so a future
/// provider's costs can never be silently summed against USD budget ceilings. A future non-USD
/// provider must make its own explicit currency/exchange-rate decision; there is no conversion here.
/// </summary>
public sealed class UnsupportedProviderCurrencyException : Exception
{
    public UnsupportedProviderCurrencyException(string currency)
        : base($"AI budgets are USD-only; the provider's currency '{currency}' is not supported.")
    {
    }
}
