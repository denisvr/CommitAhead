using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class CreateStudyItemUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateStudyItemUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Guid> ExecuteAsync(
        string title,
        StudyItemCategory category,
        int importance,
        int initialMastery,
        IReadOnlyList<string> tags,
        StudyItemDetails details,
        CancellationToken cancellationToken)
    {
        var item = new StudyItem(
            Guid.NewGuid(),
            _currentUser.UserId,
            title,
            category,
            importance,
            initialMastery,
            tags,
            details,
            DateTime.UtcNow);

        await _repository.AddAsync(item, cancellationToken);

        return item.Id;
    }
}
