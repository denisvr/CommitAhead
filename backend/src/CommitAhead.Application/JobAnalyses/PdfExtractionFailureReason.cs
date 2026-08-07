namespace CommitAhead.Application.JobAnalyses;

public enum PdfExtractionFailureReason
{
    Malformed,
    Encrypted,
    ImageOnly,
    TooManyPages,
    TimedOut,
    TooMuchText,
}
