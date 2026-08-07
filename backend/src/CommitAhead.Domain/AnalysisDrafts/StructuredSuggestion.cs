namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// A typed command proposal (ADR-0005) — accepting it fires a normal domain command instead of
/// touching a source entity directly. PayloadJson's expected shape depends on CommandType and is
/// validated against that shape only when an accepted decision is applied (Application layer) —
/// Domain has no per-command payload schema to check here, since each allowlisted command's exact
/// fields live with that command's own use case.
/// </summary>
public sealed class StructuredSuggestion : SuggestionPayload
{
    public StructuredSuggestionCommandType CommandType { get; }
    public string PayloadJson { get; }

    public StructuredSuggestion(StructuredSuggestionCommandType commandType, string payloadJson)
    {
        CommandType = TextValidation.ValidateDefined(commandType, nameof(commandType));
        PayloadJson = TextValidation.RequireNonBlank(payloadJson, nameof(payloadJson), ValidationLimits.StructuredSuggestionPayloadJsonMaxLength);
    }
}
