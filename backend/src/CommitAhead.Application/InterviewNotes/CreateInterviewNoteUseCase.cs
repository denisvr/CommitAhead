using CommitAhead.Application.Identity;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.InterviewNotes;

public sealed class CreateInterviewNoteUseCase
{
    private readonly IInterviewNoteRepository _repository;
    private readonly IJobAnalysisRepository _jobAnalysisRepository;
    private readonly ICurrentUser _currentUser;

    public CreateInterviewNoteUseCase(IInterviewNoteRepository repository, IJobAnalysisRepository jobAnalysisRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _jobAnalysisRepository = jobAnalysisRepository;
        _currentUser = currentUser;
    }

    /// <summary>Throws DomainValidationException (invariant 29) when jobAnalysisId is supplied but doesn't refer to the current user's own JobAnalysis — a request-validation problem, the future Api layer maps it to 422.</summary>
    public async Task<Guid> ExecuteAsync(
        string company,
        string role,
        InterviewRound interviewRound,
        int sequenceNumber,
        string? otherLabel,
        DateOnly date,
        IEnumerable<string> questions,
        IEnumerable<string> gaps,
        IEnumerable<string> lessons,
        Guid? jobAnalysisId,
        CancellationToken cancellationToken)
    {
        if (jobAnalysisId is not null)
        {
            var jobAnalysis = await _jobAnalysisRepository.GetByIdAsync(_currentUser.UserId, jobAnalysisId.Value, cancellationToken);
            if (jobAnalysis is null)
            {
                throw new DomainValidationException("jobAnalysisId does not refer to an existing JobAnalysis owned by the current user.");
            }
        }

        var note = new InterviewNote(
            Guid.NewGuid(), _currentUser.UserId, company, role, interviewRound, sequenceNumber, otherLabel, date, questions, gaps, lessons, jobAnalysisId, DateTime.UtcNow);

        await _repository.AddAsync(note, cancellationToken);

        return note.Id;
    }
}
