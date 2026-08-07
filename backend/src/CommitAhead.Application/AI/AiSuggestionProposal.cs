using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Application.AI;

/// <summary>
/// A raw AI-proposed SuggestionProposal, before the analyzing use case validates it and
/// constructs the real Domain <see cref="SuggestionProposal"/> (with a use-case-assigned Id).
/// Exactly one of (<see cref="CommandType"/> + <see cref="PayloadJson"/>) or
/// <see cref="AdvisoryMarkdown"/> must be set — the use case rejects a response where both or
/// neither are, rather than guessing which the provider meant.
/// </summary>
public sealed record AiSuggestionProposal(StructuredSuggestionCommandType? CommandType, string? PayloadJson, string? AdvisoryMarkdown);
