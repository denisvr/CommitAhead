namespace CommitAhead.Application.AI;

/// <summary>
/// Thrown when an AI provider's response fails validation — an out-of-range weight, an undefined
/// enum, an unknown JSON property, a TargetStudyItemId/RequirementId absent from the catalogue
/// that was sent, a duplicate target, an unsupported StructuredSuggestion CommandType, or an
/// unresolvable same-response requirement reference. A genuine AI-output-quality problem, never a
/// caller input error or an infrastructure failure. Never carries the raw provider response —
/// only a safe, fixed description.
/// </summary>
public sealed class AiResponseValidationException : Exception
{
    public AiResponseValidationException(string message) : base(message)
    {
    }
}
