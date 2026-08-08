namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>
/// Thrown when the decision set given to ApplyAnalysisDraftUseCase is invalid — a missing,
/// duplicate, or unknown proposal Id, inconsistent optional-field presence, an unsupported source/
/// command combination, an invalid final payload, a gap referencing a rejected requirement, an
/// already-existing or target-missing EvidenceLink, and so on (ADR-0005: "a proposal omitted or
/// listed twice makes the command invalid"). Never carries the raw JSON payload or a raw
/// JsonException message — only a fixed, safe description, optionally including a Domain
/// validation message (itself always a safe, human-authored string, e.g. "Weight must be in [0,5].").
/// </summary>
public sealed class ApplyAnalysisDraftValidationException : Exception
{
    public ApplyAnalysisDraftValidationException(string message) : base(message)
    {
    }
}
