using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>
/// The canonical payload shapes for every allowlisted StructuredSuggestion command
/// (StructuredSuggestionCommandType) — shared by both directions that ever construct one: an
/// AnalyzeX use case validating a raw AI proposal into a canonical <c>ProposedPayload</c>
/// (Application/AI/), and ApplyAnalysisDraftUseCase validating a user-finalised decision into a
/// canonical <c>AcceptedPayload</c> (this namespace). Neither depends on the other's private
/// implementation details — this is the one shared contract both reference.
/// </summary>
public sealed record AddJobRequirementCanonicalPayload(Guid AssignedRequirementId, string Text, JobRequirementKind Kind, JobRequirementPriority Priority, string SourceExcerpt);

public sealed record AddJobGapCanonicalPayload(Guid RequirementId, JobGapMatchLevel MatchLevel, JobGapSeverity Severity, string Rationale);

/// <summary><see cref="SummaryMarkdown"/> is nullable because CVPresentation.SummaryOverrideMarkdown can be cleared back to the profile's own summary — only ApplyAnalysisDraftUseCase's canonicalizer allows null; AnalyzeCVPresentationUseCase's own canonicalizer still requires non-blank text for an AI-proposed summary.</summary>
public sealed record UpdateCVPresentationSummaryPayload(string? SummaryMarkdown);

/// <summary>Shared by AddInterviewGap and AddInterviewLesson — both are a single new list entry with no other fields.</summary>
public sealed record InterviewNoteEntryPayload(string Text);
