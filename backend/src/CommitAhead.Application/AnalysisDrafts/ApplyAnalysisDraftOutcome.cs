namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>The result of one ApplyAnalysisDraftUseCase call. A malformed decision set throws ApplyAnalysisDraftValidationException instead — these are the ordinary, non-exceptional outcomes.</summary>
public enum ApplyAnalysisDraftOutcome
{
    Applied,
    DraftNotFound,
    DraftNotPending,

    /// <summary>The draft's source no longer exists — possible today because source-deletion cleanup (ADR-0011) isn't implemented yet, not just a theoretical case.</summary>
    SourceNotFound,
}
