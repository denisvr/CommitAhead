using CommitAhead.Application.Identity;

namespace CommitAhead.Application.StudyItems;

public sealed class ClearPriorityOverrideUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ClearPriorityOverrideUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<StudyItemMutationResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return StudyItemMutationResult.NotFound;
        }

        item.ClearPriorityOverride(DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return StudyItemMutationResult.Success;
    }
}
