using CommitAhead.Application.Identity;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain;
using CommitAhead.Domain.InterviewNotes;

namespace CommitAhead.Application.InterviewNotes;

public sealed class UpdateInterviewNoteUseCase
{
    private readonly IInterviewNoteRepository _repository;
    private readonly IJobAnalysisRepository _jobAnalysisRepository;
    private readonly ICurrentUser _currentUser;

    public UpdateInterviewNoteUseCase(IInterviewNoteRepository repository, IJobAnalysisRepository jobAnalysisRepository, ICurrentUser currentUser)
    {
        _repository = repository;
        _jobAnalysisRepository = jobAnalysisRepository;
        _currentUser = currentUser;
    }

    /// <summary>NotFound is for the InterviewNote itself; an invalid jobAnalysisId (invariant 29) throws DomainValidationException instead — a request-validation problem, not a "resource not found," matching Replace*SelectionsUseCase's existing precedent for cross-aggregate checks.</summary>
    public async Task<InterviewNoteMutationResult> ExecuteAsync(
        Guid id,
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
        var note = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (note is null)
        {
            return InterviewNoteMutationResult.NotFound;
        }

        if (jobAnalysisId is not null)
        {
            var jobAnalysis = await _jobAnalysisRepository.GetByIdAsync(_currentUser.UserId, jobAnalysisId.Value, cancellationToken);
            if (jobAnalysis is null)
            {
                throw new DomainValidationException("jobAnalysisId does not refer to an existing JobAnalysis owned by the current user.");
            }
        }

        note.Update(company, role, interviewRound, sequenceNumber, otherLabel, date, questions, gaps, lessons, jobAnalysisId, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return InterviewNoteMutationResult.Success;
    }
}
