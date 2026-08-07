namespace CommitAhead.Application.JobAnalyses;

/// <summary>
/// Thrown by <see cref="IPdfTextExtractor"/> for any rejected PDF. Never carries the underlying
/// parser's own exception message — only the safe, closed <see cref="PdfExtractionFailureReason"/>
/// — so a caller can log/report this without risking exposure of parser-internal detail.
/// </summary>
public sealed class PdfExtractionException : Exception
{
    public PdfExtractionFailureReason Reason { get; }

    public PdfExtractionException(PdfExtractionFailureReason reason)
        : base($"PDF extraction rejected: {reason}.")
    {
        Reason = reason;
    }
}
