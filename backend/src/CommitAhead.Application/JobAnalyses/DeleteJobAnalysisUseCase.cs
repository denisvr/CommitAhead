using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Hard delete, transactional with ADR-0011's polymorphic-source cleanup: all EvidenceLinks and
/// AnalysisDrafts (any status, including Pending) for this JobAnalysis are removed in the same
/// transaction as the JobAnalysis itself — there is no real FK to cascade this, so the application
/// must. What this deliberately does NOT do, and why:
///
/// - No InterviewNote handling: invariant 19 ("deleting a JobAnalysis nulls any InterviewNote's
///   reference to it, never deletes the note") is enforced by a real PostgreSQL FK with
///   ON DELETE SET NULL on interview_notes.job_analysis_id, verified with a real-Postgres
///   integration test in the Infrastructure slice — not simulated here with application code
///   against a second repository.
///
/// What it DOES do: if the deleted analysis's JobSource was an UploadedFile, its Storage object is
/// deleted best-effort AFTER the DB transaction commits (ADR-0011's exact ordering — Storage and
/// PostgreSQL don't share a transaction boundary). A failure here is logged and swallowed; the DB
/// rows are already gone, which ADR-0011 accepts as the failure mode (an orphaned Storage object,
/// not a stuck deletion).
/// </summary>
public sealed class DeleteJobAnalysisUseCase
{
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly IJobAnalysisRepository _repository;
    private readonly IEvidenceLinkRepository _evidenceLinkRepository;
    private readonly IAnalysisDraftRepository _analysisDraftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobPostingStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteJobAnalysisUseCase> _logger;

    public DeleteJobAnalysisUseCase(
        IJobAnalysisRepository repository, IEvidenceLinkRepository evidenceLinkRepository, IAnalysisDraftRepository analysisDraftRepository,
        IUnitOfWork unitOfWork, IJobPostingStorage storage, ICurrentUser currentUser, ILogger<DeleteJobAnalysisUseCase> logger)
    {
        _repository = repository;
        _evidenceLinkRepository = evidenceLinkRepository;
        _analysisDraftRepository = analysisDraftRepository;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<JobAnalysisMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;
        var analysis = await _repository.GetByIdAsync(ownerUserId, id, cancellationToken);
        if (analysis is null)
        {
            return JobAnalysisMutationResult.NotFound;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _evidenceLinkRepository.DeleteAllForSourceAsync(ownerUserId, EvidenceSourceType.JobAnalysis, id, ct);
                await _analysisDraftRepository.DeleteAllForSourceAsync(ownerUserId, EvidenceSourceType.JobAnalysis, id, ct);
                await _repository.DeleteAsync(analysis, ct);
                await _repository.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

        if (analysis.JobSource is UploadedFile uploadedFile)
        {
            await CleanUpStorageAsync(uploadedFile.StorageObjectKey);
        }

        return JobAnalysisMutationResult.Success;
    }

    private async Task CleanUpStorageAsync(string storageObjectKey)
    {
        try
        {
            using var cleanupCts = new CancellationTokenSource(CleanupTimeout);
            await _storage.DeleteAsync(storageObjectKey, cleanupCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Best-effort Storage cleanup failed after deleting a JobAnalysis. The database row is already gone; the Storage object is orphaned for manual cleanup. StorageObjectKey: {StorageObjectKey}. Exception type: {ExceptionType}",
                storageObjectKey, ex.GetType().Name);
        }
    }
}
