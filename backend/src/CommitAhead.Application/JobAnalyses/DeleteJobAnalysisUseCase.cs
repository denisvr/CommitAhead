using CommitAhead.Application.Identity;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Unconditional hard delete — nothing guards JobAnalysis deletion. What this deliberately does
/// NOT do, and why:
///
/// - No InterviewNote handling: invariant 19 ("deleting a JobAnalysis nulls any InterviewNote's
///   reference to it, never deletes the note") is enforced by a real PostgreSQL FK with
///   ON DELETE SET NULL on interview_notes.job_analysis_id, verified with a real-Postgres
///   integration test in the Infrastructure slice — not simulated here with application code
///   against a second repository.
/// - No EvidenceLink/AnalysisDraft cleanup (ADR-0011): moved to Phase 4 (docs/roadmap.md) —
///   EvidenceLink has no creation path and AnalysisDraft doesn't exist at all until then, so there
///   is nothing a JobAnalysis deletion could need to clean up yet. Same treatment already given to
///   DeleteCVPresentationUseCase in Phase 2.
///
/// What it DOES do: if the deleted analysis's JobSource was an UploadedFile, its Storage object is
/// deleted best-effort AFTER the DB delete commits (ADR-0011's exact ordering — Storage and
/// PostgreSQL don't share a transaction boundary). A failure here is logged and swallowed; the DB
/// row is already gone, which ADR-0011 accepts as the failure mode (an orphaned Storage object,
/// not a stuck deletion).
/// </summary>
public sealed class DeleteJobAnalysisUseCase
{
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly IJobAnalysisRepository _repository;
    private readonly IJobPostingStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteJobAnalysisUseCase> _logger;

    public DeleteJobAnalysisUseCase(
        IJobAnalysisRepository repository, IJobPostingStorage storage, ICurrentUser currentUser, ILogger<DeleteJobAnalysisUseCase> logger)
    {
        _repository = repository;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<JobAnalysisMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (analysis is null)
        {
            return JobAnalysisMutationResult.NotFound;
        }

        await _repository.DeleteAsync(analysis, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

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
                "Best-effort Storage cleanup failed after deleting a JobAnalysis. The database row is already gone; the Storage object is orphaned for manual cleanup. Exception type: {ExceptionType}",
                ex.GetType().Name);
        }
    }
}
