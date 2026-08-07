using CommitAhead.Application.Identity;

namespace CommitAhead.Application.InterviewNotes;

public sealed class GetInterviewNotesUseCase
{
    private readonly IInterviewNoteRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetInterviewNotesUseCase(IInterviewNoteRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InterviewNoteResult>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var notes = await _repository.GetAllAsync(_currentUser.UserId, cancellationToken);
        return notes.Select(InterviewNoteResult.FromDomain).ToList();
    }
}
