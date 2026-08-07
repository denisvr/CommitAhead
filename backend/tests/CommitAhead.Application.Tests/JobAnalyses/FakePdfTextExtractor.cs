using CommitAhead.Application.JobAnalyses;

namespace CommitAhead.Application.Tests.JobAnalyses;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2. Records the exact bytes it received so tests can assert both consumers of the use case's buffer see the same complete content.</summary>
public sealed class FakePdfTextExtractor : IPdfTextExtractor
{
    public byte[]? ReceivedBytes { get; private set; }

    public string TextToReturn { get; set; } = "Extracted job posting text.";

    public Exception? ExceptionToThrow { get; set; }

    /// <summary>Invoked immediately before throwing <see cref="ExceptionToThrow"/> — lets a test simulate a caller cancellation firing mid-extraction by cancelling the same CancellationTokenSource the use case itself was called with.</summary>
    public Action? BeforeThrow { get; set; }

    public async Task<string> ExtractTextAsync(Stream pdfContent, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await pdfContent.CopyToAsync(buffer, cancellationToken);
        ReceivedBytes = buffer.ToArray();

        if (ExceptionToThrow is not null)
        {
            BeforeThrow?.Invoke();
            throw ExceptionToThrow;
        }

        return TextToReturn;
    }
}
