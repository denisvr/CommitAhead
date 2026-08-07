using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeJobAnalysisRepository : IJobAnalysisRepository
{
    private readonly List<JobAnalysis> _analyses = [];

    public IReadOnlyList<JobAnalysis> Analyses => _analyses;

    public Task<JobAnalysis?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var analysis = _analyses.SingleOrDefault(a => a.OwnerUserId == ownerUserId && a.Id == id);
        return Task.FromResult(analysis);
    }

    public Task<IReadOnlyList<JobAnalysis>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<JobAnalysis> analyses = _analyses.Where(a => a.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(analyses);
    }

    public Task AddAsync(JobAnalysis analysis, CancellationToken cancellationToken)
    {
        _analyses.Add(analysis);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(JobAnalysis analysis, CancellationToken cancellationToken)
    {
        _analyses.RemoveAll(a => a.Id == analysis.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
