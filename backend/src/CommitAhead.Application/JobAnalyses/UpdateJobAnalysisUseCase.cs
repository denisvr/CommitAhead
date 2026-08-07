using CommitAhead.Application.Identity;

namespace CommitAhead.Application.JobAnalyses;

public sealed class UpdateJobAnalysisUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public UpdateJobAnalysisUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<JobAnalysisMutationResult> ExecuteAsync(Guid id, string title, string? notesMarkdown, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (analysis is null)
        {
            return JobAnalysisMutationResult.NotFound;
        }

        analysis.Update(title, notesMarkdown, DateTime.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);

        return JobAnalysisMutationResult.Success;
    }
}
