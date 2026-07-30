using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class SetPriorityOverrideUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public SetPriorityOverrideUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<StudyItemMutationResult> ExecuteAsync(Guid id, int score, string reason, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return StudyItemMutationResult.NotFound;
        }

        item.SetPriorityOverride(new PriorityOverride(score, reason), DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return StudyItemMutationResult.Success;
    }
}
