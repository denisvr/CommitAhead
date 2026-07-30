using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public sealed class FakeScoringConfigRepository : IScoringConfigRepository
{
    private readonly Dictionary<Guid, ScoringWeights> _overridesByOwner = [];

    public Task<ScoringWeights?> GetOverrideAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_overridesByOwner.GetValueOrDefault(ownerUserId));
    }

    public Task SetOverrideAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken)
    {
        _overridesByOwner[ownerUserId] = weights;
        return Task.CompletedTask;
    }

    public Task ResetAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        _overridesByOwner.Remove(ownerUserId);
        return Task.CompletedTask;
    }
}
