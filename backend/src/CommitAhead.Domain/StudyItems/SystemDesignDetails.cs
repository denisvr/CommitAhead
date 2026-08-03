namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Revealing ReferenceSolutionMarkdown in the UI is transient client-side state and is never
/// persisted or tracked here (docs/domain/model.md).
/// </summary>
public sealed class SystemDesignDetails : StudyItemDetails
{
    public string PromptMarkdown { get; }
    public IReadOnlyList<string> ClarifyingQuestions { get; }
    public IReadOnlyList<string> FunctionalRequirements { get; }
    public IReadOnlyList<string> NonFunctionalRequirements { get; }
    public IReadOnlyList<string> EvaluationChecklist { get; }
    public string ReferenceSolutionMarkdown { get; }

    public SystemDesignDetails(
        string promptMarkdown,
        IEnumerable<string> clarifyingQuestions,
        IEnumerable<string> functionalRequirements,
        IEnumerable<string> nonFunctionalRequirements,
        IEnumerable<string> evaluationChecklist,
        string referenceSolutionMarkdown)
    {
        PromptMarkdown = TextValidation.RequireNonBlank(promptMarkdown, nameof(promptMarkdown), ValidationLimits.MarkdownMaxLength);
        ClarifyingQuestions = TextValidation.RequireEntries(clarifyingQuestions, nameof(clarifyingQuestions));
        FunctionalRequirements = TextValidation.RequireEntries(functionalRequirements, nameof(functionalRequirements));
        NonFunctionalRequirements = TextValidation.RequireEntries(nonFunctionalRequirements, nameof(nonFunctionalRequirements));
        EvaluationChecklist = TextValidation.RequireEntries(evaluationChecklist, nameof(evaluationChecklist));
        ReferenceSolutionMarkdown = TextValidation.RequireNonBlank(referenceSolutionMarkdown, nameof(referenceSolutionMarkdown), ValidationLimits.MarkdownMaxLength);
    }
}
