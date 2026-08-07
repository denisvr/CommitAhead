using CommitAhead.Application.Identity;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Takes an already-constructed JobSource directly, the same way CreateStudyItemUseCase already
/// takes StudyItemDetails directly (Phase 1) — no branching on PastedText vs UploadedFile here.
///
/// Trust boundary, recorded for whoever builds the Api layer next: a PastedText's Content came
/// directly from the user, so it needs no special handling. An UploadedFile's StorageObjectKey and
/// ExtractedText must NEVER be constructed from raw, client-supplied API request fields — they can
/// only come from a trusted, backend-controlled upload+extraction flow (quarantine-key generation,
/// PDF parsing under strict limits) that does not exist yet. This use case has no way to tell a
/// trustworthy UploadedFile from a fabricated one; it is the caller's responsibility to never wire
/// a request DTO straight into `new UploadedFile(...)`.
/// </summary>
public sealed class CreateJobAnalysisUseCase
{
    private readonly IJobAnalysisRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateJobAnalysisUseCase(IJobAnalysisRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Guid> ExecuteAsync(string title, JobSource jobSource, string? notesMarkdown, CancellationToken cancellationToken)
    {
        var analysis = new JobAnalysis(Guid.NewGuid(), _currentUser.UserId, title, jobSource, notesMarkdown, DateTime.UtcNow);

        await _repository.AddAsync(analysis, cancellationToken);

        return analysis.Id;
    }
}
