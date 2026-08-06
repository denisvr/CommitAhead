using CommitAhead.Application.Identity;

namespace CommitAhead.Application.CVPresentations;

public sealed class GetCVPresentationsUseCase
{
    private readonly ICVPresentationRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetCVPresentationsUseCase(ICVPresentationRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CVPresentationResult>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var presentations = await _repository.GetAllAsync(_currentUser.UserId, cancellationToken);
        return presentations.Select(CVPresentationResult.FromDomain).ToList();
    }
}
