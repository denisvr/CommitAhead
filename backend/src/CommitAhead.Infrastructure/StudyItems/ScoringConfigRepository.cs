using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.StudyItems;

public sealed class ScoringConfigRepository : IScoringConfigRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public ScoringConfigRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ScoringWeights?> GetOverrideAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var row = await FindRowAsync(ownerUserId, cancellationToken);
        return row is null ? null : new ScoringWeights(row.ImportanceWeight, row.DemandWeight, row.MasteryGapWeight);
    }

    public async Task SetOverrideAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken)
    {
        var row = await FindRowAsync(ownerUserId, cancellationToken);
        if (row is null)
        {
            _dbContext.ScoringConfigOverrides.Add(new ScoringConfigOverrideRow
            {
                OwnerUserId = ownerUserId,
                ImportanceWeight = weights.ImportanceWeight,
                DemandWeight = weights.DemandWeight,
                MasteryGapWeight = weights.MasteryGapWeight,
            });
        }
        else
        {
            row.ImportanceWeight = weights.ImportanceWeight;
            row.DemandWeight = weights.DemandWeight;
            row.MasteryGapWeight = weights.MasteryGapWeight;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var row = await FindRowAsync(ownerUserId, cancellationToken);
        if (row is not null)
        {
            _dbContext.ScoringConfigOverrides.Remove(row);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<ScoringConfigOverrideRow?> FindRowAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return _dbContext.ScoringConfigOverrides.SingleOrDefaultAsync(row => row.OwnerUserId == ownerUserId, cancellationToken);
    }
}
