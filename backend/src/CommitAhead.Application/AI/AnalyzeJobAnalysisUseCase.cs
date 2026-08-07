using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Identity;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.AI;

/// <summary>
/// Triggers one AnalyzeJobAnalysis AI command (ADR-0005) and produces an AnalysisDraft. Every
/// non-happy path is an explicit <see cref="AnalyzeJobAnalysisOutcome"/>, never an exception —
/// idempotency-key replay (ADR-0014: <see cref="AnalyzeJobAnalysisOutcome.AlreadyCompleted"/>/
/// <see cref="AnalyzeJobAnalysisOutcome.InProgress"/>/<see cref="AnalyzeJobAnalysisOutcome.FailedPreviously"/>,
/// which must retry with a new key), a concurrent reservation for the same owner under a
/// different key (<see cref="AnalyzeJobAnalysisOutcome.AnotherAnalysisInProgress"/> — ADR-0015: the
/// "one AI call in flight" lock is per owner, never global), a missing source
/// (<see cref="AnalyzeJobAnalysisOutcome.SourceNotFound"/>), or an already-Pending draft for this
/// source (<see cref="AnalyzeJobAnalysisOutcome.DraftAlreadyPending"/>).
///
/// Two transaction boundaries, both via <see cref="IUnitOfWork"/>: the reservation step (reconciles
/// a stale Reserved record for this owner, then inserts the new one — ADR-0014's lazy
/// reconciliation, done inline here, no background worker), and the draft/completion step (the
/// AnalysisDraft and the AIUsageRecord's Complete() commit together or not at all). On any failure
/// after a successful reservation, the usage record is reconciled to Failed by re-reading it fresh
/// (never reusing the in-memory instance mutated by an aborted Complete() call — see
/// <see cref="ReconcileFailureAsync"/>) using a short independent cancellation token, and the
/// original exception always propagates unmasked.
/// </summary>
public sealed class AnalyzeJobAnalysisUseCase
{
    private static readonly TimeSpan StaleReservationSafetyMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReconciliationTimeout = TimeSpan.FromSeconds(5);

    private readonly IJobAnalysisRepository _jobAnalysisRepository;
    private readonly IAnalysisDraftRepository _draftRepository;
    private readonly IAIUsageRecordRepository _usageRepository;
    private readonly IStudyItemRepository _studyItemRepository;
    private readonly IProfessionalProfileRepository _profileRepository;
    private readonly IAIProvider _aiProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AnalyzeJobAnalysisUseCase> _logger;

    public AnalyzeJobAnalysisUseCase(
        IJobAnalysisRepository jobAnalysisRepository,
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IStudyItemRepository studyItemRepository,
        IProfessionalProfileRepository profileRepository,
        IAIProvider aiProvider,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AnalyzeJobAnalysisUseCase> logger)
    {
        _jobAnalysisRepository = jobAnalysisRepository;
        _draftRepository = draftRepository;
        _usageRepository = usageRepository;
        _studyItemRepository = studyItemRepository;
        _profileRepository = profileRepository;
        _aiProvider = aiProvider;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AnalyzeJobAnalysisResult> ExecuteAsync(Guid jobAnalysisId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;

        var replay = await _usageRepository.GetByIdempotencyKeyAsync(ownerUserId, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return MapReplay(replay);
        }

        var jobAnalysis = await _jobAnalysisRepository.GetByIdAsync(ownerUserId, jobAnalysisId, cancellationToken);
        if (jobAnalysis is null)
        {
            return new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.SourceNotFound, null);
        }

        var pendingDraft = await _draftRepository.GetPendingBySourceAsync(ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysisId, cancellationToken);
        if (pendingDraft is not null)
        {
            return new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.DraftAlreadyPending, null);
        }

        var descriptor = _aiProvider.Describe(AiCommandType.AnalyzeJobAnalysis);
        var limits = new AiCallLimits(descriptor.MaxInputTokens, descriptor.MaxOutputTokens, descriptor.Timeout);

        var reservedAtUtc = DateTime.UtcNow;
        var reservation = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, idempotencyKey, AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, jobAnalysisId,
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
            return concurrent is not null ? MapReplay(concurrent) : new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.AnotherAnalysisInProgress, null);
        }

        var existingRequirements = jobAnalysis.Requirements.Select(r => new JobRequirementCatalogueEntry(r.Id, r.Text)).ToList();
        var input = await BuildInputAsync(jobAnalysis, ownerUserId, existingRequirements, cancellationToken);

        try
        {
            var aiResult = await _aiProvider.AnalyzeJobAnalysisAsync(input, limits, cancellationToken)
                ?? throw new AiResponseValidationException("The AI provider returned a null result.");

            var suggestionProposals = AiStructuredSuggestionValidator.ValidateAndBuild(aiResult.SuggestionProposals, existingRequirements);
            var linkProposals = ValidateLinkProposals(aiResult.LinkProposals, input.StudyItemCatalogue);
            var studyItemProposals = ValidateStudyItemProposals(aiResult.StudyItemProposals);

            var draftId = Guid.NewGuid();
            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    var completedAtUtc = DateTime.UtcNow;
                    var draft = new AnalysisDraft(draftId, ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysisId, suggestionProposals, linkProposals, studyItemProposals, completedAtUtc);
                    await _draftRepository.AddAsync(draft, ct);

                    reservation.Complete(aiResult.InputTokens, aiResult.OutputTokens, aiResult.ActualCost, draftId, "success", completedAtUtc);
                    await _usageRepository.SaveChangesAsync(ct);
                    return true;
                },
                cancellationToken);

            return new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.Created, draftId);
        }
        catch (Exception ex)
        {
            await ReconcileFailureAsync(ownerUserId, idempotencyKey, ex);
            throw;
        }
    }

    private async Task<JobAnalysisAiInput> BuildInputAsync(
        JobAnalysis jobAnalysis, Guid ownerUserId, IReadOnlyList<JobRequirementCatalogueEntry> existingRequirements, CancellationToken cancellationToken)
    {
        var jobPostingText = jobAnalysis.JobSource switch
        {
            PastedText pastedText => pastedText.Content,
            UploadedFile uploadedFile => uploadedFile.ExtractedText,
            _ => throw new InvalidOperationException($"Unrecognized JobSource subtype '{jobAnalysis.JobSource.GetType().Name}'."),
        };

        var profile = await _profileRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        var profileSkills = profile?.Skills.Select(s => s.DisplayName).ToList() ?? [];

        var studyItems = await _studyItemRepository.GetAllAsync(ownerUserId, cancellationToken);
        var studyItemCatalogue = studyItems
            .Where(item => item.Status == StudyItemStatus.Active)
            .Select(item => new StudyItemCatalogueEntry(item.Id, item.Title, item.Category, item.Tags))
            .ToList();

        return new JobAnalysisAiInput(jobPostingText, profileSkills, studyItemCatalogue, existingRequirements);
    }

    private static AnalyzeJobAnalysisResult MapReplay(AIUsageRecord record) => record.Status switch
    {
        AIUsageRecordStatus.Completed => new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.AlreadyCompleted, record.AnalysisDraftId),
        AIUsageRecordStatus.Reserved => new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.InProgress, null),
        AIUsageRecordStatus.Failed => new AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome.FailedPreviously, null),
        _ => throw new InvalidOperationException($"Unrecognized AIUsageRecordStatus '{record.Status}'."),
    };

    private static IReadOnlyList<LinkProposal> ValidateLinkProposals(IReadOnlyList<AiLinkProposal> rawProposals, IReadOnlyList<StudyItemCatalogueEntry> catalogue)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("LinkProposals must not be null.");
        }

        var catalogueIds = catalogue.Select(entry => entry.Id).ToHashSet();
        var seenTargets = new HashSet<Guid>();
        var result = new List<LinkProposal>(rawProposals.Count);

        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("LinkProposals must not contain a null entry.");
            }

            if (!catalogueIds.Contains(raw.TargetStudyItemId))
            {
                throw new AiResponseValidationException("LinkProposal.TargetStudyItemId does not match a known StudyItem.");
            }

            if (!seenTargets.Add(raw.TargetStudyItemId))
            {
                throw new AiResponseValidationException("Duplicate LinkProposal.TargetStudyItemId in the same response.");
            }

            result.Add(Validate(() => new LinkProposal(Guid.NewGuid(), raw.TargetStudyItemId, raw.Weight, raw.Rationale)));
        }

        return result;
    }

    private static IReadOnlyList<StudyItemProposal> ValidateStudyItemProposals(IReadOnlyList<AiStudyItemProposal> rawProposals)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("StudyItemProposals must not be null.");
        }

        var result = new List<StudyItemProposal>(rawProposals.Count);
        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("StudyItemProposals must not contain a null entry.");
            }

            var details = AiStudyItemDetailsParser.Parse(raw.Category, raw.DetailsJson);
            result.Add(Validate(() => new StudyItemProposal(Guid.NewGuid(), raw.Title, raw.Category, details, raw.Tags, raw.Importance)));
        }

        return result;
    }

    private static T Validate<T>(Func<T> construct)
    {
        try
        {
            return construct();
        }
        catch (DomainValidationException ex)
        {
            throw new AiResponseValidationException($"AI proposal failed validation: {ex.Message}");
        }
    }

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
