namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>Free-form text with no automatic effect — accepting it only marks it for manual follow-up (ADR-0005).</summary>
public sealed class AdvisorySuggestion : SuggestionPayload
{
    public string Markdown { get; }

    public AdvisorySuggestion(string markdown)
    {
        Markdown = TextValidation.RequireNonBlank(markdown, nameof(markdown), ValidationLimits.AdvisoryMarkdownMaxLength);
    }
}
