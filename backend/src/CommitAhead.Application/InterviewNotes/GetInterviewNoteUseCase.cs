using CommitAhead.Application.Identity;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.InterviewNotes;

public sealed class GetInterviewNoteUseCase
{
    private readonly IInterviewNoteRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetInterviewNoteUseCase(IInterviewNoteRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<InterviewNoteResult?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        return note is null ? null : InterviewNoteResult.FromDomain(note);
    }
}

public sealed record InterviewNoteResult(
    Guid Id,
    string Company,
    string Role,
    InterviewRound InterviewRound,
    int SequenceNumber,
    string? OtherLabel,
    DateOnly Date,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Lessons,
    Guid? JobAnalysisId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static InterviewNoteResult FromDomain(InterviewNote note) => new(
        note.Id,
        note.Company,
        note.Role,
        note.InterviewRound,
        note.SequenceNumber,
        note.OtherLabel,
        note.Date,
        note.Questions,
        note.Gaps,
        note.Lessons,
        note.JobAnalysisId,
        note.CreatedAtUtc,
        note.UpdatedAtUtc);
}
