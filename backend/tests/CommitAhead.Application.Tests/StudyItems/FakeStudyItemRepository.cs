using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2. Scoping by ownerUserId mirrors the real repository.</summary>
public sealed class FakeStudyItemRepository : IStudyItemRepository
{
    private readonly List<StudyItem> _items = [];

    public IReadOnlyList<StudyItem> Items => _items;

    /// <summary>Simulates the database rejecting a delete (a concurrent Restrict FK violation) so DeleteStudyItemUseCase's mapping to Blocked can be tested without EF Core.</summary>
    public bool RejectNextDelete { get; set; }

    public Task<StudyItem?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var item = _items.SingleOrDefault(i => i.OwnerUserId == ownerUserId && i.Id == id);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<StudyItem>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<StudyItem> items = _items.Where(i => i.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(items);
    }

    public Task AddAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(StudyItem item, CancellationToken cancellationToken)
    {
        if (RejectNextDelete)
        {
            RejectNextDelete = false;
            return Task.FromResult(false);
        }

        _items.RemoveAll(i => i.Id == item.Id);
        return Task.FromResult(true);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
