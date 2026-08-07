using CommitAhead.Application.InterviewNotes;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.Tests.InterviewNotes;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeInterviewNoteRepository : IInterviewNoteRepository
{
    private readonly List<InterviewNote> _notes = [];

    public IReadOnlyList<InterviewNote> Notes => _notes;

    public Task<InterviewNote?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        var note = _notes.SingleOrDefault(n => n.OwnerUserId == ownerUserId && n.Id == id);
        return Task.FromResult(note);
    }

    public Task<IReadOnlyList<InterviewNote>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<InterviewNote> notes = _notes.Where(n => n.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(notes);
    }

    public Task AddAsync(InterviewNote note, CancellationToken cancellationToken)
    {
        _notes.Add(note);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(InterviewNote note, CancellationToken cancellationToken)
    {
        _notes.RemoveAll(n => n.Id == note.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
