using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.InterviewNotes;

public interface IInterviewNoteRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's InterviewNote (ADR-0015).</summary>
    Task<InterviewNote?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<InterviewNote>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken);

    Task AddAsync(InterviewNote note, CancellationToken cancellationToken);

    Task DeleteAsync(InterviewNote note, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through InterviewNote's own methods on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
