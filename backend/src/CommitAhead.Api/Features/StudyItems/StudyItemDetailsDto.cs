using System.Text.Json.Serialization;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Api.Features.StudyItems;

/// <summary>
/// The wire-contract counterpart of StudyItemDetails (ADR-0001's discriminated union). Kept
/// separate from Infrastructure's own DTOs for the same union — Api must not depend on
/// Infrastructure — but mirrors its "kind" discriminator so the shape is consistent end to end.
/// Not a "*Controller" type, so it may reference Domain types freely (NetArchTest rule 4 only
/// restricts controllers themselves); all Domain-touching for StudyItems lives here, never in a
/// controller body.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(LeetCodeDetailsDto), "LeetCode")]
[JsonDerivedType(typeof(SystemDesignDetailsDto), "SystemDesign")]
[JsonDerivedType(typeof(BehavioralDetailsDto), "Behavioral")]
[JsonDerivedType(typeof(TheoryDetailsDto), "Theory")]
public abstract record StudyItemDetailsDto
{
    public abstract StudyItemDetails ToDomain();

    public static StudyItemDetailsDto FromDomain(StudyItemDetails details) => details switch
    {
        LeetCodeDetails d => LeetCodeDetailsDto.FromDomain(d),
        SystemDesignDetails d => SystemDesignDetailsDto.FromDomain(d),
        BehavioralDetails d => BehavioralDetailsDto.FromDomain(d),
        TheoryDetails d => TheoryDetailsDto.FromDomain(d),
        _ => throw new ArgumentOutOfRangeException(nameof(details), $"Unknown StudyItemDetails type '{details.GetType().Name}'."),
    };
}

public sealed record LeetCodeDetailsDto(
    int? ProblemNumber,
    string? Url,
    Difficulty Difficulty,
    IReadOnlyList<string> Patterns,
    string ExpectedTimeComplexity,
    string ExpectedSpaceComplexity,
    string ApproachMarkdown,
    string? CSharpSolution) : StudyItemDetailsDto
{
    public static LeetCodeDetailsDto FromDomain(LeetCodeDetails d) => new(
        d.ProblemNumber, d.Url, d.Difficulty, d.Patterns, d.ExpectedTimeComplexity, d.ExpectedSpaceComplexity, d.ApproachMarkdown, d.CSharpSolution);

    public override StudyItemDetails ToDomain() => new LeetCodeDetails(
        ProblemNumber, Url, Difficulty, Patterns, ExpectedTimeComplexity, ExpectedSpaceComplexity, ApproachMarkdown, CSharpSolution);
}

public sealed record SystemDesignDetailsDto(
    string PromptMarkdown,
    IReadOnlyList<string> ClarifyingQuestions,
    IReadOnlyList<string> FunctionalRequirements,
    IReadOnlyList<string> NonFunctionalRequirements,
    IReadOnlyList<string> EvaluationChecklist,
    string ReferenceSolutionMarkdown) : StudyItemDetailsDto
{
    public static SystemDesignDetailsDto FromDomain(SystemDesignDetails d) => new(
        d.PromptMarkdown, d.ClarifyingQuestions, d.FunctionalRequirements, d.NonFunctionalRequirements, d.EvaluationChecklist, d.ReferenceSolutionMarkdown);

    public override StudyItemDetails ToDomain() => new SystemDesignDetails(
        PromptMarkdown, ClarifyingQuestions, FunctionalRequirements, NonFunctionalRequirements, EvaluationChecklist, ReferenceSolutionMarkdown);
}

public sealed record BehavioralDetailsDto(
    IReadOnlyList<string> Competencies,
    IReadOnlyList<string> QuestionVariants,
    string Situation,
    string Task,
    string Action,
    string Result,
    string? Reflection) : StudyItemDetailsDto
{
    public static BehavioralDetailsDto FromDomain(BehavioralDetails d) => new(
        d.Competencies, d.QuestionVariants, d.Situation, d.Task, d.Action, d.Result, d.Reflection);

    public override StudyItemDetails ToDomain() => new BehavioralDetails(Competencies, QuestionVariants, Situation, Task, Action, Result, Reflection);
}

public sealed record TheoryDetailsDto(
    string SummaryMarkdown,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> InterviewQuestions,
    IReadOnlyList<string> References) : StudyItemDetailsDto
{
    public static TheoryDetailsDto FromDomain(TheoryDetails d) => new(d.SummaryMarkdown, d.KeyPoints, d.InterviewQuestions, d.References);

    public override StudyItemDetails ToDomain() => new TheoryDetails(SummaryMarkdown, KeyPoints, InterviewQuestions, References);
}
