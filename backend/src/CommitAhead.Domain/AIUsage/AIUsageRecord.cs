using CommitAhead.Domain;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Domain.AIUsage;

/// <summary>
/// A pre-call budget reservation, reconciled after the provider call completes or fails
/// (ADR-0014). Never stores prompt or response content — only safe metadata (provider/model,
/// pricing version, token counts, a closed OutcomeCode). IdempotencyKey uniqueness and the
/// daily/monthly budget check are enforced by the reservation transaction (Application +
/// Infrastructure), not here — this aggregate has no way to see other records.
/// </summary>
public sealed class AIUsageRecord
{
    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string IdempotencyKey { get; }
    public AiCommandType CommandType { get; }
    public EvidenceSourceType SourceType { get; }
    public Guid SourceId { get; }
    public string Provider { get; }
    public string Model { get; }
    public string PricingVersion { get; }
    public string Currency { get; }
    public AIUsageRecordStatus Status { get; private set; }
    public int ReservedInputTokens { get; }
    public int ReservedOutputTokens { get; }
    public decimal ReservedCost { get; }
    public int? ActualInputTokens { get; private set; }
    public int? ActualOutputTokens { get; private set; }
    public decimal? ActualCost { get; private set; }
    public Guid? AnalysisDraftId { get; private set; }
    public DateTime StartedAtUtc { get; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? OutcomeCode { get; private set; }

    public AIUsageRecord(
        Guid id,
        Guid ownerUserId,
        string idempotencyKey,
        AiCommandType commandType,
        EvidenceSourceType sourceType,
        Guid sourceId,
        string provider,
        string model,
        string pricingVersion,
        string currency,
        int reservedInputTokens,
        int reservedOutputTokens,
        decimal reservedCost,
        DateTime startedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (sourceId == Guid.Empty)
        {
            throw new DomainValidationException("SourceId is required.");
        }

        if (reservedInputTokens < 0 || reservedOutputTokens < 0)
        {
            throw new DomainValidationException("Reserved token counts must not be negative.");
        }

        if (reservedCost < 0)
        {
            throw new DomainValidationException("ReservedCost must not be negative.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        IdempotencyKey = TextValidation.RequireNonBlank(idempotencyKey, nameof(idempotencyKey), ValidationLimits.IdempotencyKeyMaxLength);
        CommandType = TextValidation.ValidateDefined(commandType, nameof(commandType));
        SourceType = TextValidation.ValidateDefined(sourceType, nameof(sourceType));
        SourceId = sourceId;
        Provider = TextValidation.RequireNonBlank(provider, nameof(provider), ValidationLimits.ProviderMaxLength);
        Model = TextValidation.RequireNonBlank(model, nameof(model), ValidationLimits.ModelMaxLength);
        PricingVersion = TextValidation.RequireNonBlank(pricingVersion, nameof(pricingVersion), ValidationLimits.PricingVersionMaxLength);
        Currency = ValidateCurrency(currency);
        ReservedInputTokens = reservedInputTokens;
        ReservedOutputTokens = reservedOutputTokens;
        ReservedCost = reservedCost;
        Status = AIUsageRecordStatus.Reserved;
        StartedAtUtc = startedAtUtc;
    }

    public void Complete(int actualInputTokens, int actualOutputTokens, decimal actualCost, Guid analysisDraftId, string? outcomeCode, DateTime completedAtUtc)
    {
        EnsureReserved();

        if (actualInputTokens < 0 || actualOutputTokens < 0)
        {
            throw new DomainValidationException("Actual token counts must not be negative.");
        }

        if (actualCost < 0)
        {
            throw new DomainValidationException("ActualCost must not be negative.");
        }

        if (analysisDraftId == Guid.Empty)
        {
            throw new DomainValidationException("AnalysisDraftId is required to complete a usage record.");
        }

        ActualInputTokens = actualInputTokens;
        ActualOutputTokens = actualOutputTokens;
        ActualCost = actualCost;
        AnalysisDraftId = analysisDraftId;
        OutcomeCode = TextValidation.TrimToNullOrValidate(outcomeCode, nameof(outcomeCode), ValidationLimits.OutcomeCodeMaxLength);
        Status = AIUsageRecordStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void Fail(string? outcomeCode, DateTime completedAtUtc)
    {
        EnsureReserved();

        OutcomeCode = TextValidation.TrimToNullOrValidate(outcomeCode, nameof(outcomeCode), ValidationLimits.OutcomeCodeMaxLength);
        Status = AIUsageRecordStatus.Failed;
        CompletedAtUtc = completedAtUtc;
    }

    private void EnsureReserved()
    {
        if (Status != AIUsageRecordStatus.Reserved)
        {
            throw new DomainValidationException("Only a Reserved usage record can transition.");
        }
    }

    private static string ValidateCurrency(string currency)
    {
        var trimmed = TextValidation.RequireNonBlank(currency, nameof(currency), ValidationLimits.CurrencyCodeLength);
        if (trimmed.Length != ValidationLimits.CurrencyCodeLength)
        {
            throw new DomainValidationException($"currency must be exactly {ValidationLimits.CurrencyCodeLength} characters (ISO 4217).");
        }

        return trimmed.ToUpperInvariant();
    }
}
