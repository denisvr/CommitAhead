using System.Text.Json;
using System.Text.Json.Serialization;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Infrastructure.StudyItems;

/// <summary>
/// Owns the "kind" discriminator and the mapping to/from each concrete StudyItemDetails subtype.
/// The Domain types carry no serialization attributes and never reference System.Text.Json —
/// this converter, and the DTOs below, are the entire boundary (docs/architecture/persistence.md,
/// "Typed category details").
/// </summary>
internal sealed class StudyItemDetailsJsonConverter : JsonConverter<StudyItemDetails>
{
    private static readonly JsonSerializerOptions DtoOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public override StudyItemDetails Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();

        return kind switch
        {
            "LeetCode" => root.Deserialize<LeetCodeDetailsDto>(DtoOptions)!.ToDomain(),
            "SystemDesign" => root.Deserialize<SystemDesignDetailsDto>(DtoOptions)!.ToDomain(),
            "Behavioral" => root.Deserialize<BehavioralDetailsDto>(DtoOptions)!.ToDomain(),
            "Theory" => root.Deserialize<TheoryDetailsDto>(DtoOptions)!.ToDomain(),
            _ => throw new JsonException($"Unknown StudyItemDetails kind '{kind}'."),
        };
    }

    public override void Write(Utf8JsonWriter writer, StudyItemDetails value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case LeetCodeDetails details:
                JsonSerializer.Serialize(writer, LeetCodeDetailsDto.FromDomain(details), DtoOptions);
                return;
            case SystemDesignDetails details:
                JsonSerializer.Serialize(writer, SystemDesignDetailsDto.FromDomain(details), DtoOptions);
                return;
            case BehavioralDetails details:
                JsonSerializer.Serialize(writer, BehavioralDetailsDto.FromDomain(details), DtoOptions);
                return;
            case TheoryDetails details:
                JsonSerializer.Serialize(writer, TheoryDetailsDto.FromDomain(details), DtoOptions);
                return;
            default:
                throw new JsonException($"Unknown StudyItemDetails type '{value.GetType().Name}'.");
        }
    }

    private sealed record LeetCodeDetailsDto(
        string Kind,
        int? ProblemNumber,
        string? Url,
        Difficulty Difficulty,
        List<string> Patterns,
        string ExpectedTimeComplexity,
        string ExpectedSpaceComplexity,
        string ApproachMarkdown,
        string? CSharpSolution)
    {
        public static LeetCodeDetailsDto FromDomain(LeetCodeDetails details) => new(
            "LeetCode", details.ProblemNumber, details.Url, details.Difficulty, details.Patterns.ToList(),
            details.ExpectedTimeComplexity, details.ExpectedSpaceComplexity, details.ApproachMarkdown, details.CSharpSolution);

        public LeetCodeDetails ToDomain() => new(
            ProblemNumber, Url, Difficulty, Patterns, ExpectedTimeComplexity, ExpectedSpaceComplexity, ApproachMarkdown, CSharpSolution);
    }

    private sealed record SystemDesignDetailsDto(
        string Kind,
        string PromptMarkdown,
        List<string> ClarifyingQuestions,
        List<string> FunctionalRequirements,
        List<string> NonFunctionalRequirements,
        List<string> EvaluationChecklist,
        string ReferenceSolutionMarkdown)
    {
        public static SystemDesignDetailsDto FromDomain(SystemDesignDetails details) => new(
            "SystemDesign", details.PromptMarkdown, details.ClarifyingQuestions.ToList(), details.FunctionalRequirements.ToList(),
            details.NonFunctionalRequirements.ToList(), details.EvaluationChecklist.ToList(), details.ReferenceSolutionMarkdown);

        public SystemDesignDetails ToDomain() => new(
            PromptMarkdown, ClarifyingQuestions, FunctionalRequirements, NonFunctionalRequirements, EvaluationChecklist, ReferenceSolutionMarkdown);
    }

    private sealed record BehavioralDetailsDto(
        string Kind,
        List<string> Competencies,
        List<string> QuestionVariants,
        string Situation,
        string Task,
        string Action,
        string Result,
        string? Reflection)
    {
        public static BehavioralDetailsDto FromDomain(BehavioralDetails details) => new(
            "Behavioral", details.Competencies.ToList(), details.QuestionVariants.ToList(),
            details.Situation, details.Task, details.Action, details.Result, details.Reflection);

        public BehavioralDetails ToDomain() => new(Competencies, QuestionVariants, Situation, Task, Action, Result, Reflection);
    }

    private sealed record TheoryDetailsDto(
        string Kind,
        string SummaryMarkdown,
        List<string> KeyPoints,
        List<string> InterviewQuestions,
        List<string> References)
    {
        public static TheoryDetailsDto FromDomain(TheoryDetails details) => new(
            "Theory", details.SummaryMarkdown, details.KeyPoints.ToList(), details.InterviewQuestions.ToList(), details.References.ToList());

        public TheoryDetails ToDomain() => new(SummaryMarkdown, KeyPoints, InterviewQuestions, References);
    }
}
