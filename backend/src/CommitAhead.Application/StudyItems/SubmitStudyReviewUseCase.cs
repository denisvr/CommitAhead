using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class SubmitStudyReviewUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly ICurrentUser _currentUser;

    public SubmitStudyReviewUseCase(IStudyItemRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<StudyItemMutationResult> ExecuteAsync(Guid id, int confidenceRating, string? notesMarkdown, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return StudyItemMutationResult.NotFound;
        }

        var now = DateTime.UtcNow;
        item.AddReview(new StudyReview(Guid.NewGuid(), now, confidenceRating, notesMarkdown), now);
        await _repository.SaveChangesAsync(cancellationToken);

        return StudyItemMutationResult.Success;
    }
}
