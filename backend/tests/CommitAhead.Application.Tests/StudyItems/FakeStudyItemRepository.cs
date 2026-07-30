using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2. Scoping by ownerUserId mirrors the real repository.</summary>
public sealed class FakeStudyItemRepository : IStudyItemRepository
{
    private readonly List<StudyItem> _items = [];

    public IReadOnlyList<StudyItem> Items => _items;

    public Task<StudyItem?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var item = _items.SingleOrDefault(i => i.OwnerUserId == ownerUserId && i.Id == id);
        return Task.FromResult(item);
    }

    public Task AddAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _items.RemoveAll(i => i.Id == item.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
