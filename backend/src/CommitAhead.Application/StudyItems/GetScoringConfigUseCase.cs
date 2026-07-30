using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class GetScoringConfigUseCase
{
    private readonly IScoringConfigRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetScoringConfigUseCase(IScoringConfigRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ScoringConfigResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var weights = await _repository.GetOverrideAsync(_currentUser.UserId, cancellationToken);
        var isOverridden = weights is not null;
        weights ??= ScoringWeights.Default;

        return new ScoringConfigResult(weights.ImportanceWeight, weights.DemandWeight, weights.MasteryGapWeight, isOverridden);
    }
}

public sealed record ScoringConfigResult(int ImportanceWeight, int DemandWeight, int MasteryGapWeight, bool IsOverridden);
