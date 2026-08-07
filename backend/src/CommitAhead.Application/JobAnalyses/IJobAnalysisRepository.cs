using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.JobAnalyses;

public interface IJobAnalysisRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's JobAnalysis (ADR-0015).</summary>
    Task<JobAnalysis?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobAnalysis>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken);

    Task AddAsync(JobAnalysis analysis, CancellationToken cancellationToken);

    Task DeleteAsync(JobAnalysis analysis, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through JobAnalysis's own methods on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
