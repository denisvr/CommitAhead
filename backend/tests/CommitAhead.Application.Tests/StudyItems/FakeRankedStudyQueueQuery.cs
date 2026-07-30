using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.StudyItems;

public sealed class FakeRankedStudyQueueQuery : IRankedStudyQueueQuery
{
    public IReadOnlyList<RankedStudyItem> ResultToReturn { get; set; } = [];
    public Guid? LastOwnerUserId { get; private set; }
    public ScoringWeights? LastWeights { get; private set; }

    public Task<IReadOnlyList<RankedStudyItem>> ExecuteAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken)
    {
        LastOwnerUserId = ownerUserId;
        LastWeights = weights;
        return Task.FromResult(ResultToReturn);
    }
}
