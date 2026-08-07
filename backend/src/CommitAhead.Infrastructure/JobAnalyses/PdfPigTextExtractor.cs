using System.Text;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain.JobAnalyses;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace CommitAhead.Infrastructure.JobAnalyses;

/// <summary>
/// PdfPig 0.1.15, read/text-extraction APIs only — never rendering, image extraction,
/// annotations, embedded links, scripts, or network access (docs/tbd.md).
///
/// PdfPig's own API is entirely synchronous and takes no <see cref="CancellationToken"/>, so the
/// 10-second budget below is a best-effort wall-clock race via <c>Task.WaitAsync</c>, not a hard
/// parser-level timeout: if the wait times out or the caller cancels, the background
/// <see cref="Task.Run(Func{object})"/> is not actually aborted — it may keep running to
/// completion on its own thread pool thread regardless of which outcome this method returns. This
/// is a documented, accepted residual risk for this phase (container memory/CPU limits are the
/// real backstop), not a guarantee — no worker process or parser sandbox is introduced to close it.
///
/// Because that background parse can outlive this call, it must never touch anything the caller
/// might dispose or reuse afterward: the input <see cref="Stream"/> is fully read into an owned
/// <c>byte[]</c> before <c>Task.Run</c> starts, and only that owned array — never the caller's
/// stream — crosses into the background task. PdfPig's own <see cref="PdfDocument.Open(byte[])"/>
/// overload takes that array directly, so there is no intermediate <see cref="MemoryStream"/> for
/// the background task to own or dispose either — one fewer thing that could outlive this call.
/// </summary>
public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    private const int MaxPages = 20;
    private static readonly TimeSpan ExtractionTimeout = TimeSpan.FromSeconds(10);

    public async Task<string> ExtractTextAsync(Stream pdfContent, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await pdfContent.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        Task<string> parseTask = Task.Run(() => ParseSync(bytes));

        try
        {
            return await parseTask.WaitAsync(ExtractionTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new PdfExtractionException(PdfExtractionFailureReason.TimedOut);
        }
        // A genuine caller cancellation (cancellationToken fired before the timeout) propagates
        // unchanged from WaitAsync as an OperationCanceledException — never reinterpreted as a
        // timeout.
    }

    private static string ParseSync(byte[] bytes)
    {
        try
        {
            using var document = PdfDocument.Open(bytes);

            if (document.IsEncrypted)
            {
                throw new PdfExtractionException(PdfExtractionFailureReason.Encrypted);
            }

            if (document.NumberOfPages > MaxPages)
            {
                throw new PdfExtractionException(PdfExtractionFailureReason.TooManyPages);
            }

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                // A newline between pages, not a bare append — otherwise the last word of one
                // page and the first word of the next would read as a single merged word, since
                // PdfPig's own page.Text carries no trailing separator of its own.
                if (text.Length > 0)
                {
                    text.Append('\n');
                }

                text.Append(NormalizeLineEndings(page.Text));
                if (text.Length > ValidationLimits.JobSourceTextMaxLength)
                {
                    // Reject, never truncate (ADR-0010) — stop as soon as the cap is crossed,
                    // rather than finishing every remaining page first.
                    throw new PdfExtractionException(PdfExtractionFailureReason.TooMuchText);
                }
            }

            var trimmed = text.ToString().Trim();
            if (trimmed.Length == 0)
            {
                throw new PdfExtractionException(PdfExtractionFailureReason.ImageOnly);
            }

            return trimmed;
        }
        catch (PdfExtractionException)
        {
            // Our own explicit rejections above — pass through unchanged, never remapped by the
            // catch-all below.
            throw;
        }
        catch (PdfDocumentEncryptedException)
        {
            throw new PdfExtractionException(PdfExtractionFailureReason.Encrypted);
        }
        catch (PdfDocumentFormatException)
        {
            throw new PdfExtractionException(PdfExtractionFailureReason.Malformed);
        }
        // No catch-all: anything other than the two known PdfPig failure types above (a corrupt
        // font table, an unexpected internal parser bug, etc.) is a genuine infrastructure
        // failure, not a validation problem — it propagates unchanged rather than being silently
        // reclassified as Malformed.
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
}
