namespace CommitAhead.Domain.StudyItems;

public sealed class BehavioralDetails : StudyItemDetails
{
    public IReadOnlyList<string> Competencies { get; }
    public IReadOnlyList<string> QuestionVariants { get; }
    public string Situation { get; }
    public string Task { get; }
    public string Action { get; }
    public string Result { get; }
    public string? Reflection { get; }

    public BehavioralDetails(
        IEnumerable<string> competencies,
        IEnumerable<string> questionVariants,
        string situation,
        string task,
        string action,
        string result,
        string? reflection)
    {
        Competencies = TextValidation.RequireEntries(competencies, nameof(competencies));
        QuestionVariants = TextValidation.RequireEntries(questionVariants, nameof(questionVariants));
        Situation = TextValidation.RequireNonBlank(situation, nameof(situation), ValidationLimits.MarkdownMaxLength);
        Task = TextValidation.RequireNonBlank(task, nameof(task), ValidationLimits.MarkdownMaxLength);
        Action = TextValidation.RequireNonBlank(action, nameof(action), ValidationLimits.MarkdownMaxLength);
        Result = TextValidation.RequireNonBlank(result, nameof(result), ValidationLimits.MarkdownMaxLength);
        Reflection = TextValidation.TrimToNullOrValidate(reflection, nameof(reflection), ValidationLimits.MarkdownMaxLength);
    }
}
