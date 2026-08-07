namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Text-only extraction from an uploaded PDF (ADR-0010) — never rendering, image extraction,
/// annotations, embedded links, scripts, or network access. Enforces the page-count/timeout/
/// character-count limits itself; rejects explicitly rather than truncating. Throws
/// <see cref="PdfExtractionException"/> for every rejection.
/// </summary>
public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfContent, CancellationToken cancellationToken);
}
