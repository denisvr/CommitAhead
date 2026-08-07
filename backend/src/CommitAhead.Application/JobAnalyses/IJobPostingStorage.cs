namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Supabase Storage, backend-mediated. The caller (not this port) generates the quarantine key —
/// see <see cref="CreateJobAnalysisFromUploadUseCase"/> — so a failed upload can still be cleaned
/// up by its known key even if <see cref="UploadAsync"/> itself throws partway through.
/// </summary>
public interface IJobPostingStorage
{
    Task UploadAsync(string storageObjectKey, Stream content, string mimeType, CancellationToken cancellationToken);

    /// <summary>Best-effort; deleting a key that was never successfully stored is harmless (ADR-0011's accepted orphan-cleanup model).</summary>
    Task DeleteAsync(string storageObjectKey, CancellationToken cancellationToken);
}
