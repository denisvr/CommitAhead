namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// A typed command proposal (ADR-0005) — accepting it fires a normal domain command instead of
/// touching a source entity directly. PayloadJson's expected shape depends on CommandType — Domain
/// has no per-command payload schema to check here, since each allowlisted command's exact fields
/// live with that command's own use case. That schema is validated twice, by two different
/// callers: once here, at analysis time (the analyzing use case strict-parses the AI's raw output
/// into the canonical shape before this type is ever constructed — untrusted AI-generated IDs are
/// never stored; e.g. AnalyzeJobAnalysisUseCase assigns its own Guid for a same-response
/// AddJobRequirement/AddJobGap pair), and again before application (ApplyAnalysisDraftUseCase,
/// future work, must not blindly trust a persisted PayloadJson either). ApplyAnalysisDraft must
/// also apply an accepted AddJobRequirement before any accepted AddJobGap that references it, and
/// must reject applying a gap whose referenced requirement proposal was not itself accepted.
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
