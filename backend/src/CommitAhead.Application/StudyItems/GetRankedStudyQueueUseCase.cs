using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class GetRankedStudyQueueUseCase
{
    private readonly IRankedStudyQueueQuery _query;
    private readonly IScoringConfigRepository _scoringConfigRepository;
    private readonly ICurrentUser _currentUser;

    public GetRankedStudyQueueUseCase(IRankedStudyQueueQuery query, IScoringConfigRepository scoringConfigRepository, ICurrentUser currentUser)
    {
        _query = query;
        _scoringConfigRepository = scoringConfigRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RankedStudyItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var weights = await _scoringConfigRepository.GetOverrideAsync(_currentUser.UserId, cancellationToken) ?? ScoringWeights.Default;

        return await _query.ExecuteAsync(_currentUser.UserId, weights, cancellationToken);
    }
}
