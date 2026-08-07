using CommitAhead.Application.Identity;

namespace CommitAhead.Application.JobAnalyses;

public sealed class GetJobAnalysesUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetJobAnalysesUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<JobAnalysisResult>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var analyses = await _repository.GetAllAsync(_currentUser.UserId, cancellationToken);
        return analyses.Select(JobAnalysisResult.FromDomain).ToList();
    }
}
