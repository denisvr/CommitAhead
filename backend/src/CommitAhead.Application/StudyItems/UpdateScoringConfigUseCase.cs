using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class UpdateScoringConfigUseCase
{
    private readonly IScoringConfigRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateScoringConfigUseCase(IScoringConfigRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task ExecuteAsync(int importanceWeight, int demandWeight, int masteryGapWeight, CancellationToken cancellationToken)
    {
        var weights = new ScoringWeights(importanceWeight, demandWeight, masteryGapWeight);
        await _repository.SetOverrideAsync(_currentUser.UserId, weights, cancellationToken);
    }
}
