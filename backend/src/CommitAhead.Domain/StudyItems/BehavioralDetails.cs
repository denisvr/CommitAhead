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
        Competencies = competencies.ToList();
        QuestionVariants = questionVariants.ToList();
        Situation = situation;
        Task = task;
        Action = action;
        Result = result;
        Reflection = reflection;
    }
}
