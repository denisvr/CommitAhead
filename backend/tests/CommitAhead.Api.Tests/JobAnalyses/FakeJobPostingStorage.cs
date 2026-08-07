using CommitAhead.Application.JobAnalyses;

namespace CommitAhead.Api.Tests.JobAnalyses;

/// <summary>Replaces the real SupabaseStorageClient (which makes real HTTP calls) so API tests can exercise the upload endpoint without a network call — zero real calls, per testing/strategy.md.</summary>
public sealed class FakeJobPostingStorage : IJobPostingStorage
{
    public List<(string Key, byte[] Bytes, string MimeType)> UploadCalls { get; } = [];

    public List<string> DeletedKeys { get; } = [];

    public async Task UploadAsync(string storageObjectKey, Stream content, string mimeType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        UploadCalls.Add((storageObjectKey, buffer.ToArray(), mimeType));
    }

    public Task DeleteAsync(string storageObjectKey, CancellationToken cancellationToken)
    {
        DeletedKeys.Add(storageObjectKey);
        return Task.CompletedTask;
    }
}
