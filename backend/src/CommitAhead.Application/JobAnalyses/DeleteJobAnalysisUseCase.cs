using CommitAhead.Application.Identity;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Unconditional hard delete — nothing guards JobAnalysis deletion. What this deliberately does
/// NOT do, and why:
///
/// - No InterviewNote handling: invariant 19 ("deleting a JobAnalysis nulls any InterviewNote's
///   reference to it, never deletes the note") is enforced by a real PostgreSQL FK with
///   ON DELETE SET NULL on interview_notes.job_analysis_id, added and verified with a real-Postgres
///   integration test in the Infrastructure slice — not simulated here with application code
///   against a second repository.
/// - No EvidenceLink/AnalysisDraft cleanup (ADR-0011): moved to Phase 4 (docs/roadmap.md) —
///   EvidenceLink has no creation path and AnalysisDraft doesn't exist at all until then, so there
///   is nothing a JobAnalysis deletion could need to clean up yet. Same treatment already given to
///   DeleteCVPresentationUseCase in Phase 2.
/// - No Storage cleanup for an UploadedFile source — no Storage client exists yet; that lands with
///   the upload-flow Infrastructure slice.
/// </summary>
public sealed class DeleteJobAnalysisUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DeleteJobAnalysisUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
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

        return JobAnalysisMutationResult.Success;
    }
}
