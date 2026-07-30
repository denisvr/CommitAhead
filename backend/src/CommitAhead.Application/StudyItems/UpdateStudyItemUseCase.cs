using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class UpdateStudyItemUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateStudyItemUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<StudyItemMutationResult> ExecuteAsync(
        Guid id,
        string title,
        int importance,
        IEnumerable<string> tags,
        StudyItemDetails details,
        CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return StudyItemMutationResult.NotFound;
        }

        item.Update(title, importance, tags, details, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return StudyItemMutationResult.Success;
    }
}
