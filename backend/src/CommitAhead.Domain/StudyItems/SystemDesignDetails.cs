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
        PromptMarkdown = promptMarkdown;
        ClarifyingQuestions = clarifyingQuestions.ToList();
        FunctionalRequirements = functionalRequirements.ToList();
        NonFunctionalRequirements = nonFunctionalRequirements.ToList();
        EvaluationChecklist = evaluationChecklist.ToList();
        ReferenceSolutionMarkdown = referenceSolutionMarkdown;
    }
}
