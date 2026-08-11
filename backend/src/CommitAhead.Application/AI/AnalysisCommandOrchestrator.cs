using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Persistence;
using CommitAhead.Domain;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using Microsoft.Extensions.Logging;
using AIUsageValidationLimits = CommitAhead.Domain.AIUsage.ValidationLimits;

namespace CommitAhead.Application.AI;

/// <summary>
/// The reservation/concurrency/transaction lifecycle every AnalyzeX use case shares (ADR-0005,
/// ADR-0014, ADR-0015) — extracted once all three commands needed it identically, so a correctness
/// fix (concurrency, idempotency, atomicity) lands once, not three times. Each concrete AnalyzeX
/// use case supplies only what's genuinely source-specific: fetching/validating its own source
/// (SourceNotFound is checked by the caller, before this runs), building its own minimised input,
/// calling its own IAIProvider method, and validating its own StructuredSuggestion allowlist.
///
/// Idempotency-key replay (<see cref="AnalyzeCommandOutcome.AlreadyCompleted"/>/
/// <see cref="AnalyzeCommandOutcome.InProgress"/>/<see cref="AnalyzeCommandOutcome.FailedPreviously"/>,
/// ADR-0014 — a Failed key must retry with a new one), a concurrent reservation for the same owner
/// under a different key (<see cref="AnalyzeCommandOutcome.AnotherAnalysisInProgress"/> — ADR-0015:
/// the "one AI call in flight" lock is per owner, never global), and an already-Pending draft for
/// this source (<see cref="AnalyzeCommandOutcome.DraftAlreadyPending"/>) are all resolved without
/// ever calling the AI provider.
///
/// Three independently-committed owner-scoped transactions, all via <see cref="IRlsSessionContext"/>
/// (ADR-0014): the reservation phase (reconciles a stale Reserved record for this owner, checks the
/// per-owner daily/monthly budget, then inserts the new Reserved record — committed before the
/// provider is ever called), the completion phase (the AnalysisDraft and the AIUsageRecord's
/// Complete() commit together, strictly after the provider call returns), and — only on failure — a
/// short failure-reconciliation phase that marks the already-committed reservation Failed. No
/// database transaction is held open during the external AI call: a crash or timeout there can never
/// roll back a reservation the provider has already accepted/billed. On any failure after a
/// successful reservation, the usage record is reconciled to Failed by re-reading it fresh (never
/// reusing the in-memory instance an aborted Complete() call mutated — see
/// <see cref="ReconcileFailureAsync"/>) using a short independent cancellation token, and the
/// original exception always propagates unmasked.
/// </summary>
internal sealed class AnalysisCommandOrchestrator
{
    private static readonly TimeSpan StaleReservationSafetyMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReconciliationTimeout = TimeSpan.FromSeconds(5);

    private readonly IAnalysisDraftRepository _draftRepository;
    private readonly IAIUsageRecordRepository _usageRepository;
    private readonly IAIProvider _aiProvider;
    private readonly IRlsSessionContext _rlsSessionContext;
    private readonly ILogger _logger;

    public AnalysisCommandOrchestrator(
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IAIProvider aiProvider,
        IRlsSessionContext rlsSessionContext,
        ILogger logger)
    {
        _draftRepository = draftRepository;
        _usageRepository = usageRepository;
        _aiProvider = aiProvider;
        _rlsSessionContext = rlsSessionContext;
        _logger = logger;
    }

    public async Task<AnalyzeCommandResult> ExecuteAsync(
        Guid ownerUserId,
        string idempotencyKey,
        AiCommandType commandType,
        EvidenceSourceType sourceType,
        Guid sourceId,
        Func<AiCallLimits, CancellationToken, Task<AiAnalysisResult>> invokeProviderAsync,
        Func<AiAnalysisResult, AnalysisDraftProposals> validate,
        CancellationToken cancellationToken)
    {
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        var reservedAtUtc = DateTime.UtcNow;

        ReservationPhaseResult phase;
        try
        {
            phase = await _rlsSessionContext.RunInOwnerScopeAsync(
                ownerUserId,
                ct => RunReservationPhaseAsync(ownerUserId, normalizedIdempotencyKey, commandType, sourceType, sourceId, reservedAtUtc, ct),
                cancellationToken);
        }
        catch (AIUsageReservationConflictException)
        {
            var concurrent = await _rlsSessionContext.RunInOwnerScopeAsync(
                ownerUserId, ct => _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, normalizedIdempotencyKey, ct), cancellationToken);
            return concurrent is not null ? MapReplay(concurrent) : new AnalyzeCommandResult(AnalyzeCommandOutcome.AnotherAnalysisInProgress, null);
        }

        switch (phase.Outcome)
        {
            case ReservationPhaseOutcome.Replay:
                return MapReplay(phase.ReplayRecord!);
            case ReservationPhaseOutcome.DraftAlreadyPending:
                // Carries the existing Pending draft's Id so a caller that lost track of it (a
                // refresh, a lost navigation state) can still recover and review it, instead of
                // being told a draft exists somewhere with no way back to it.
                return new AnalyzeCommandResult(AnalyzeCommandOutcome.DraftAlreadyPending, phase.PendingDraftId);
            case ReservationPhaseOutcome.DailyBudgetExceeded:
                return new AnalyzeCommandResult(AnalyzeCommandOutcome.DailyBudgetExceeded, null);
            case ReservationPhaseOutcome.MonthlyBudgetExceeded:
                return new AnalyzeCommandResult(AnalyzeCommandOutcome.MonthlyBudgetExceeded, null);
        }

        var reservation = phase.Reservation!;
        var descriptor = phase.Descriptor!;
        var limits = phase.Limits!;

        try
        {
            var aiResult = await invokeProviderAsync(limits, cancellationToken)
                ?? throw new AiResponseValidationException("The AI provider returned a null result.");

            // Defence in depth: never persist a Completed record whose reported usage exceeds what
            // was actually reserved — a provider bug or a compromised/misbehaving implementation
            // must not be able to record (and be billed for) more than the descriptor allowed.
            if (aiResult.InputTokens > descriptor.MaxInputTokens || aiResult.OutputTokens > descriptor.MaxOutputTokens)
            {
                throw new AiResponseValidationException("The AI provider reported token usage exceeding its own reserved limits.");
            }

            var proposals = validate(aiResult);

            var draftId = Guid.NewGuid();
            await _rlsSessionContext.RunInOwnerScopeAsync(
                ownerUserId,
                async ct =>
                {
                    var completedAtUtc = DateTime.UtcNow;
                    var draft = new AnalysisDraft(
                        draftId, ownerUserId, sourceType, sourceId,
                        proposals.SuggestionProposals, proposals.LinkProposals, proposals.StudyItemProposals, completedAtUtc);
                    await _draftRepository.AddAsync(draft, ct);

                    reservation.Complete(aiResult.InputTokens, aiResult.OutputTokens, aiResult.ActualCost, draftId, "success", completedAtUtc);
                    await _usageRepository.SaveChangesAsync(ct);
                    return true;
                },
                cancellationToken);

            return new AnalyzeCommandResult(AnalyzeCommandOutcome.Created, draftId);
        }
        catch (Exception ex)
        {
            await ReconcileFailureAsync(ownerUserId, normalizedIdempotencyKey, ex);
            throw;
        }
    }

    /// <summary>
    /// Everything the reservation phase needs, in one owner-scoped transaction, committed before the
    /// provider is ever called (ADR-0014): idempotency-key replay, an already-Pending draft for this
    /// source, the provider's currency (ADR-0019 budgets are USD-only — see
    /// <see cref="UnsupportedProviderCurrencyException"/>), stale-reservation reconciliation, the
    /// daily/monthly budget check, and the reservation insert itself.
    /// </summary>
    private async Task<ReservationPhaseResult> RunReservationPhaseAsync(
        Guid ownerUserId, string normalizedIdempotencyKey, AiCommandType commandType, EvidenceSourceType sourceType, Guid sourceId,
        DateTime reservedAtUtc, CancellationToken cancellationToken)
    {
        var replay = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, normalizedIdempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return ReservationPhaseResult.ForReplay(replay);
        }

        var pendingDraft = await _draftRepository.GetPendingBySourceAsync(ownerUserId, sourceType, sourceId, cancellationToken);
        if (pendingDraft is not null)
        {
            return ReservationPhaseResult.ForDraftAlreadyPending(pendingDraft.Id);
        }

        var descriptor = _aiProvider.Describe(commandType);
        if (!string.Equals(descriptor.Currency, "USD", StringComparison.Ordinal))
        {
            throw new UnsupportedProviderCurrencyException(descriptor.Currency);
        }

        var limits = new AiCallLimits(descriptor.MaxInputTokens, descriptor.MaxOutputTokens, descriptor.Timeout);

        var staleCutoffUtc = reservedAtUtc - (descriptor.Timeout + StaleReservationSafetyMargin);
        var activeReservation = await _usageRepository.GetActiveReservationByOwnerAsync(ownerUserId, cancellationToken);
        if (activeReservation is not null && activeReservation.StartedAtUtc < staleCutoffUtc)
        {
            activeReservation.Fail("stale-reservation-timeout", reservedAtUtc);
            await _usageRepository.SaveChangesAsync(cancellationToken);
        }

        // ADR-0019: USD 0.25/day, USD 5.00/month, per owner — checked here, inside the same
        // transaction as the insert, before any provider call. The per-owner "one Reserved row at a
        // time" unique index already serializes concurrent attempts for this owner, so no extra
        // locking is needed for this check.
        var todayStartUtc = reservedAtUtc.Date;
        var dailySpent = await _usageRepository.GetSpentCostAsync(ownerUserId, todayStartUtc, todayStartUtc.AddDays(1), cancellationToken);
        if (dailySpent + descriptor.EstimatedMaxCost > AiBudgetLimits.DailyLimitUsd)
        {
            return ReservationPhaseResult.ForOutcome(ReservationPhaseOutcome.DailyBudgetExceeded);
        }

        var monthStartUtc = new DateTime(reservedAtUtc.Year, reservedAtUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlySpent = await _usageRepository.GetSpentCostAsync(ownerUserId, monthStartUtc, monthStartUtc.AddMonths(1), cancellationToken);
        if (monthlySpent + descriptor.EstimatedMaxCost > AiBudgetLimits.MonthlyLimitUsd)
        {
            return ReservationPhaseResult.ForOutcome(ReservationPhaseOutcome.MonthlyBudgetExceeded);
        }

        var reservation = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, normalizedIdempotencyKey, commandType, sourceType, sourceId,
            descriptor.Provider, descriptor.Model, descriptor.PricingVersion, descriptor.Currency,
            descriptor.MaxInputTokens, descriptor.MaxOutputTokens, descriptor.EstimatedMaxCost, reservedAtUtc);
        await _usageRepository.AddAsync(reservation, cancellationToken);

        return ReservationPhaseResult.ForReservation(reservation, descriptor, limits);
    }

    /// <summary>
    /// Trims and validates once, before any lookup, so " key " and "key" resolve to the same
    /// replay/reservation instead of silently being treated as different idempotency keys.
    /// </summary>
    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainValidationException("IdempotencyKey is required.");
        }

        var trimmed = idempotencyKey.Trim();
        if (trimmed.Length > AIUsageValidationLimits.IdempotencyKeyMaxLength)
        {
            throw new DomainValidationException($"IdempotencyKey must be at most {AIUsageValidationLimits.IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }

    private enum ReservationPhaseOutcome
    {
        Replay,
        DraftAlreadyPending,
        DailyBudgetExceeded,
        MonthlyBudgetExceeded,
        Reserved,
    }

    private sealed record ReservationPhaseResult(
        ReservationPhaseOutcome Outcome, AIUsageRecord? ReplayRecord, Guid? PendingDraftId, AIUsageRecord? Reservation, AiProviderDescriptor? Descriptor, AiCallLimits? Limits)
    {
        public static ReservationPhaseResult ForReplay(AIUsageRecord replayRecord) =>
            new(ReservationPhaseOutcome.Replay, replayRecord, null, null, null, null);

        public static ReservationPhaseResult ForDraftAlreadyPending(Guid pendingDraftId) =>
            new(ReservationPhaseOutcome.DraftAlreadyPending, null, pendingDraftId, null, null, null);

        public static ReservationPhaseResult ForOutcome(ReservationPhaseOutcome outcome) =>
            new(outcome, null, null, null, null, null);

        public static ReservationPhaseResult ForReservation(AIUsageRecord reservation, AiProviderDescriptor descriptor, AiCallLimits limits) =>
            new(ReservationPhaseOutcome.Reserved, null, null, reservation, descriptor, limits);
    }

    private static AnalyzeCommandResult MapReplay(AIUsageRecord record) => record.Status switch
    {
        AIUsageRecordStatus.Completed => new AnalyzeCommandResult(AnalyzeCommandOutcome.AlreadyCompleted, record.AnalysisDraftId),
        AIUsageRecordStatus.Reserved => new AnalyzeCommandResult(AnalyzeCommandOutcome.InProgress, null),
        AIUsageRecordStatus.Failed => new AnalyzeCommandResult(AnalyzeCommandOutcome.FailedPreviously, null),
        _ => throw new InvalidOperationException($"Unrecognized AIUsageRecordStatus '{record.Status}'."),
    };

    /// <summary>
    /// Never reuses the in-memory `reservation` instance from the failed attempt — if Complete()
    /// ran and its transaction then rolled back, Postgres reverts to Reserved but the tracked C#
    /// object still reads Completed, and calling Fail() on it would throw. Re-reading fresh in its
    /// own owner-scoped transaction (ADR-0014 — the durable reservation from the already-committed
    /// reservation phase is what gets marked Failed here) always returns a correctly-Reserved
    /// instance. Uses a short independent cancellation token — never the caller's own, which may
    /// already be the reason for this failure. Swallows its own failure; the original exception is
    /// what the caller always sees.
    /// </summary>
    private async Task ReconcileFailureAsync(Guid ownerUserId, string idempotencyKey, Exception originalException)
    {
        using var cleanupCts = new CancellationTokenSource(ReconciliationTimeout);
        Guid? recordId = null;

        try
        {
            await _rlsSessionContext.RunInOwnerScopeAsync(
                ownerUserId,
                async ct =>
                {
                    var record = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, idempotencyKey, ct);
                    recordId = record?.Id;
                    if (record is not null && record.Status == AIUsageRecordStatus.Reserved)
                    {
                        record.Fail(originalException.GetType().Name, DateTime.UtcNow);
                        await _usageRepository.SaveChangesAsync(ct);
                    }

                    return true;
                },
                cleanupCts.Token);
        }
        catch (Exception reconcileException)
        {
            _logger.LogWarning(
                "Failed to reconcile an AI usage reservation to Failed after an analysis error. RecordId: {RecordId}. ReconcileExceptionType: {ReconcileExceptionType}.",
                recordId, reconcileException.GetType().Name);
        }
    }
}
