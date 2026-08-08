using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.Tests.AI;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeAnalysisDraftRepository : IAnalysisDraftRepository
{
    private readonly List<AnalysisDraft> _drafts = [];

    public IReadOnlyList<AnalysisDraft> Drafts => _drafts;

    public Task<AnalysisDraft?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var draft = _drafts.SingleOrDefault(d => d.OwnerUserId == ownerUserId && d.Id == id);
        return Task.FromResult(draft);
    }

    public Task<AnalysisDraft?> GetPendingBySourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        var draft = _drafts.SingleOrDefault(d =>
            d.OwnerUserId == ownerUserId && d.SourceType == sourceType && d.SourceId == sourceId && d.Status == AnalysisDraftStatus.Pending);
        return Task.FromResult(draft);
    }

    /// <summary>No real locking in-memory — real concurrency is proven at the Infrastructure level (Testcontainers Postgres), not simulated here.</summary>
    public Task<AnalysisDraft?> GetByIdForUpdateAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync(ownerUserId, id, cancellationToken);

    public Task AddAsync(AnalysisDraft draft, CancellationToken cancellationToken)
    {
        _drafts.Add(draft);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
