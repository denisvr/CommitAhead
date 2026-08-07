using CommitAhead.Application.InterviewNotes;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.InterviewNotes;

public sealed class InterviewNoteRepository : IInterviewNoteRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public InterviewNoteRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InterviewNote?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.InterviewNotes
            .SingleOrDefaultAsync(note => note.OwnerUserId == ownerUserId && note.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InterviewNote>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.InterviewNotes
            .Where(note => note.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InterviewNote note, CancellationToken cancellationToken)
    {
        _dbContext.InterviewNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(InterviewNote note, CancellationToken cancellationToken)
    {
        _dbContext.InterviewNotes.Remove(note);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
