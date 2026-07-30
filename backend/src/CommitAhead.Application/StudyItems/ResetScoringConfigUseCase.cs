using CommitAhead.Application.Identity;

namespace CommitAhead.Application.StudyItems;

public sealed class ResetScoringConfigUseCase
{
    private readonly IScoringConfigRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ResetScoringConfigUseCase(IScoringConfigRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return _repository.ResetAsync(_currentUser.UserId, cancellationToken);
    }
}
