using CommitAhead.Application.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2. Records every call (even one that goes on to throw) so tests can assert on exactly what was uploaded/deleted.</summary>
public sealed class FakeJobPostingStorage : IJobPostingStorage
{
    public List<(string Key, byte[] Bytes, string MimeType)> UploadCalls { get; } = [];

    public List<string> DeletedKeys { get; } = [];

    public Exception? ExceptionToThrowOnUpload { get; set; }

    public Exception? ExceptionToThrowOnDelete { get; set; }

    public async Task UploadAsync(string storageObjectKey, Stream content, string mimeType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        UploadCalls.Add((storageObjectKey, buffer.ToArray(), mimeType));

        if (ExceptionToThrowOnUpload is not null)
        {
            throw ExceptionToThrowOnUpload;
        }
    }

    public Task DeleteAsync(string storageObjectKey, CancellationToken cancellationToken)
    {
        DeletedKeys.Add(storageObjectKey);

        if (ExceptionToThrowOnDelete is not null)
        {
            throw ExceptionToThrowOnDelete;
        }

        return Task.CompletedTask;
    }
}
