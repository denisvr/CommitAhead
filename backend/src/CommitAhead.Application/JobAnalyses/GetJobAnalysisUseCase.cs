using CommitAhead.Application.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.JobAnalyses;

public sealed class GetJobAnalysisUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetJobAnalysisUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<JobAnalysisResult?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        return analysis is null ? null : JobAnalysisResult.FromDomain(analysis);
    }
}

/// <summary>Domain child types (JobSource/JobRequirement/JobGap) pass through directly, matching ProfessionalProfileResult/StudyItemDetailResult's existing style rather than re-flattening to primitives.</summary>
public sealed record JobAnalysisResult(
    Guid Id,
    string Title,
    JobSource JobSource,
    IReadOnlyList<JobRequirement> Requirements,
    IReadOnlyList<JobGap> Gaps,
    string? NotesMarkdown,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static JobAnalysisResult FromDomain(JobAnalysis analysis) => new(
        analysis.Id,
        analysis.Title,
        analysis.JobSource,
        analysis.Requirements,
        analysis.Gaps,
        analysis.NotesMarkdown,
        analysis.CreatedAtUtc,
        analysis.UpdatedAtUtc);
}
