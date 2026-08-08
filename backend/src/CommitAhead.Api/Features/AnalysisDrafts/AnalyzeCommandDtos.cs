using System.ComponentModel.DataAnnotations;
using CommitAhead.Application.AI;
using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Api.Features.AnalysisDrafts;

/// <summary>Shared by every "analyze" action (JobAnalyses/CVPresentations/InterviewNotes) — the request/outcome shape is identical across all three AnalyzeX commands.</summary>
public sealed record AnalyzeCommandRequest(
    [Required, StringLength(ValidationLimits.IdempotencyKeyMaxLength, MinimumLength = 1)] string IdempotencyKey);

public sealed record AnalyzeCommandResponse(AnalyzeCommandOutcome Outcome, Guid? AnalysisDraftId);
