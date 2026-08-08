using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.Tests.AnalysisDrafts;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeEvidenceLinkRepository : IEvidenceLinkRepository
{
    private readonly List<EvidenceLink> _links = [];

    public IReadOnlyList<EvidenceLink> Links => _links;

    public Task<bool> ExistsAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, Guid targetStudyItemId, CancellationToken cancellationToken)
    {
        var exists = _links.Any(link =>
            link.OwnerUserId == ownerUserId && link.SourceType == sourceType && link.SourceId == sourceId && link.TargetStudyItemId == targetStudyItemId);
        return Task.FromResult(exists);
    }

    public Task<EvidenceLink?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var link = _links.SingleOrDefault(l => l.OwnerUserId == ownerUserId && l.Id == id);
        return Task.FromResult(link);
    }

    public Task AddAsync(EvidenceLink link, CancellationToken cancellationToken)
    {
        _links.Add(link);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EvidenceLink link, CancellationToken cancellationToken)
    {
        _links.Remove(link);
        return Task.CompletedTask;
    }

    public Task DeleteAllForSourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        _links.RemoveAll(l => l.OwnerUserId == ownerUserId && l.SourceType == sourceType && l.SourceId == sourceId);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
