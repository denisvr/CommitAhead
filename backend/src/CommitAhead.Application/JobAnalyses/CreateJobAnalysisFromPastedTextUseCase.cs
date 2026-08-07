using CommitAhead.Application.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Takes the posting text as a plain string and constructs the <see cref="PastedText"/> itself —
/// unlike CreateJobAnalysisFromUploadUseCase, which is the only application entry point trusted to
/// construct an <see cref="UploadedFile"/> (its StorageObjectKey and ExtractedText can never come
/// from raw, client-supplied request fields; only from a backend-controlled quarantine-key
/// generation + PDF extraction flow). Accepting a plain string here — never a generic
/// <see cref="JobSource"/> — makes that trust boundary a type-level fact instead of a convention a
/// caller has to remember.
/// </summary>
public sealed class CreateJobAnalysisFromPastedTextUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateJobAnalysisFromPastedTextUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Guid> ExecuteAsync(string title, string jobPostingText, string? notesMarkdown, CancellationToken cancellationToken)
    {
        var analysis = new JobAnalysis(Guid.NewGuid(), _currentUser.UserId, title, new PastedText(jobPostingText), notesMarkdown, DateTime.UtcNow);

        await _repository.AddAsync(analysis, cancellationToken);

        return analysis.Id;
    }
}
