using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Persistence;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using Microsoft.Extensions.Logging;

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
/// Two transaction boundaries, both via <see cref="IUnitOfWork"/>: the reservation step (reconciles
/// a stale Reserved record for this owner, then inserts the new one — ADR-0014's lazy
/// reconciliation, done inline, no background worker), and the draft/completion step (the
/// AnalysisDraft and the AIUsageRecord's Complete() commit together or not at all). On any failure
/// after a successful reservation, the usage record is reconciled to Failed by re-reading it fresh
/// (never reusing the in-memory instance an aborted Complete() call mutated — see
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _logger;

    public AnalysisCommandOrchestrator(
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IAIProvider aiProvider,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        _draftRepository = draftRepository;
        _usageRepository = usageRepository;
        _aiProvider = aiProvider;
        _unitOfWork = unitOfWork;
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
        var replay = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return MapReplay(replay);
        }

        var pendingDraft = await _draftRepository.GetPendingBySourceAsync(ownerUserId, sourceType, sourceId, cancellationToken);
        if (pendingDraft is not null)
        {
            return new AnalyzeCommandResult(AnalyzeCommandOutcome.DraftAlreadyPending, null);
        }

        var descriptor = _aiProvider.Describe(commandType);
        var limits = new AiCallLimits(descriptor.MaxInputTokens, descriptor.MaxOutputTokens, descriptor.Timeout);

        var reservedAtUtc = DateTime.UtcNow;
        var reservation = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, idempotencyKey, commandType, sourceType, sourceId,
            descriptor.Provider, descriptor.Model, descriptor.PricingVersion, descriptor.Currency,
            descriptor.MaxInputTokens, descriptor.MaxOutputTokens, descriptor.EstimatedMaxCost, reservedAtUtc);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    var staleCutoffUtc = reservedAtUtc - (descriptor.Timeout + StaleReservationSafetyMargin);
                    var activeReservation = await _usageRepository.GetActiveReservationByOwnerAsync(ownerUserId, ct);
                    if (activeReservation is not null && activeReservation.StartedAtUtc < staleCutoffUtc)
                    {
                        activeReservation.Fail("stale-reservation-timeout", reservedAtUtc);
                        await _usageRepository.SaveChangesAsync(ct);
                    }

                    await _usageRepository.AddAsync(reservation, ct);
                    return true;
                },
                cancellationToken);
        }
        catch (AIUsageReservationConflictException)
        {
            var concurrent = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, idempotencyKey, cancellationToken);
            return concurrent is not null ? MapReplay(concurrent) : new AnalyzeCommandResult(AnalyzeCommandOutcome.AnotherAnalysisInProgress, null);
        }

        try
        {
            var aiResult = await invokeProviderAsync(limits, cancellationToken)
                ?? throw new AiResponseValidationException("The AI provider returned a null result.");

            var proposals = validate(aiResult);

            var draftId = Guid.NewGuid();
            await _unitOfWork.ExecuteInTransactionAsync(
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
            await ReconcileFailureAsync(ownerUserId, idempotencyKey, ex);
            throw;
        }
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
    /// ran and the transaction then rolled back, Postgres reverts to Reserved but the tracked C#
    /// object still reads Completed, and calling Fail() on it would throw. Re-reading fresh
    /// (after IUnitOfWork's rollback has cleared the tracker) always returns a correctly-Reserved
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
            var record = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, idempotencyKey, cleanupCts.Token);
            recordId = record?.Id;
            if (record is not null && record.Status == AIUsageRecordStatus.Reserved)
            {
                record.Fail(originalException.GetType().Name, DateTime.UtcNow);
                await _usageRepository.SaveChangesAsync(cleanupCts.Token);
            }
        }
        catch (Exception reconcileException)
        {
            _logger.LogWarning(
                "Failed to reconcile an AI usage reservation to Failed after an analysis error. RecordId: {RecordId}. ReconcileExceptionType: {ReconcileExceptionType}.",
                recordId, reconcileException.GetType().Name);
        }
    }
}
