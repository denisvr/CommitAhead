using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.JobAnalyses;

public sealed class JobAnalysisRepository : IJobAnalysisRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public JobAnalysisRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<JobAnalysis?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.JobAnalyses
            .Include(analysis => analysis.Requirements)
            .Include(analysis => analysis.Gaps)
            .SingleOrDefaultAsync(analysis => analysis.OwnerUserId == ownerUserId && analysis.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<JobAnalysis>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.JobAnalyses
            .Include(analysis => analysis.Requirements)
            .Include(analysis => analysis.Gaps)
            .Where(analysis => analysis.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobAnalysis analysis, CancellationToken cancellationToken)
    {
        _dbContext.JobAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(JobAnalysis analysis, CancellationToken cancellationToken)
    {
        _dbContext.JobAnalyses.Remove(analysis);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
