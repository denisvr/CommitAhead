using CommitAhead.Application.CVPresentations;
using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeCVPresentationRepository : ICVPresentationRepository
{
    private readonly List<CVPresentation> _presentations = [];

    public IReadOnlyList<CVPresentation> Presentations => _presentations;

    public Task<CVPresentation?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var presentation = _presentations.SingleOrDefault(p => p.OwnerUserId == ownerUserId && p.Id == id);
        return Task.FromResult(presentation);
    }

    public Task<IReadOnlyList<CVPresentation>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<CVPresentation> presentations = _presentations.Where(p => p.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(presentations);
    }

    public Task AddAsync(CVPresentation presentation, CancellationToken cancellationToken)
    {
        _presentations.Add(presentation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CVPresentation presentation, CancellationToken cancellationToken)
    {
        _presentations.RemoveAll(p => p.Id == presentation.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
