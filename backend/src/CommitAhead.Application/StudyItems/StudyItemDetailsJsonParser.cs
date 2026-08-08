using System.Text.Json;
using CommitAhead.Application.Json;
using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

/// <summary>
/// Parses a category+JSON pair into a real StudyItemDetails subtype. The subtype is picked from
/// Category before parsing, so a produced Details value can never mismatch its Category —
/// StudyItem's own ValidateDetails check is then trivially satisfied. Field-level validation
/// (length caps, required entries) is provided for free by each Details subtype's own constructor.
///
/// Deliberately neutral about who's calling: an AI-proposed StudyItemProposal
/// (<c>AiStudyItemDetailsParser</c>, Application/AI/) and a user-finalised accepted
/// StudyItemProposal (ApplyAnalysisDraftUseCase, Application/AnalysisDrafts/) both parse the same
/// category+JSON shape, but each wraps a failure into its own exception type — this type never
/// throws either of those, only the neutral <see cref="StudyItemDetailsPayloadException"/>.
/// </summary>
public static class StudyItemDetailsJsonParser
{
    public static StudyItemDetails Parse(StudyItemCategory category, string detailsJson)
    {
        try
        {
            return category switch
            {
                StudyItemCategory.LeetCode => ParseLeetCode(detailsJson),
                StudyItemCategory.SystemDesign => ParseSystemDesign(detailsJson),
                StudyItemCategory.Behavioral => ParseBehavioral(detailsJson),
                StudyItemCategory.Theory => ParseTheory(detailsJson),
                _ => throw new StudyItemDetailsPayloadException($"'{category}' is not a recognized StudyItemCategory."),
            };
        }
        catch (JsonException)
        {
            throw new StudyItemDetailsPayloadException("DetailsJson is not valid JSON for the given category.");
        }
        catch (DomainValidationException ex)
        {
            throw new StudyItemDetailsPayloadException($"DetailsJson failed validation: {ex.Message}");
        }
    }

    private static StudyItemDetails ParseLeetCode(string json)
    {
        var dto = Deserialize<LeetCodeDetailsDto>(json);
        return new LeetCodeDetails(
            dto.ProblemNumber, dto.Url, dto.Difficulty, dto.Patterns, dto.ExpectedTimeComplexity,
            dto.ExpectedSpaceComplexity, dto.ApproachMarkdown, dto.CSharpSolution);
    }

    private static StudyItemDetails ParseSystemDesign(string json)
    {
        var dto = Deserialize<SystemDesignDetailsDto>(json);
        return new SystemDesignDetails(
            dto.PromptMarkdown, dto.ClarifyingQuestions, dto.FunctionalRequirements,
            dto.NonFunctionalRequirements, dto.EvaluationChecklist, dto.ReferenceSolutionMarkdown);
    }

    private static StudyItemDetails ParseBehavioral(string json)
    {
        var dto = Deserialize<BehavioralDetailsDto>(json);
        return new BehavioralDetails(dto.Competencies, dto.QuestionVariants, dto.Situation, dto.Task, dto.Action, dto.Result, dto.Reflection);
    }

    private static StudyItemDetails ParseTheory(string json)
    {
        var dto = Deserialize<TheoryDetailsDto>(json);
        return new TheoryDetails(dto.SummaryMarkdown, dto.KeyPoints, dto.InterviewQuestions, dto.References);
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, StrictJsonOptions.Strict)
        ?? throw new StudyItemDetailsPayloadException("DetailsJson must not be null.");

    private sealed record LeetCodeDetailsDto(
        int? ProblemNumber, string? Url, Difficulty Difficulty, IReadOnlyList<string> Patterns,
        string ExpectedTimeComplexity, string ExpectedSpaceComplexity, string ApproachMarkdown, string? CSharpSolution);

    private sealed record SystemDesignDetailsDto(
        string PromptMarkdown, IReadOnlyList<string> ClarifyingQuestions, IReadOnlyList<string> FunctionalRequirements,
        IReadOnlyList<string> NonFunctionalRequirements, IReadOnlyList<string> EvaluationChecklist, string ReferenceSolutionMarkdown);

    private sealed record BehavioralDetailsDto(
        IReadOnlyList<string> Competencies, IReadOnlyList<string> QuestionVariants, string Situation,
        string Task, string Action, string Result, string? Reflection);

    private sealed record TheoryDetailsDto(
        string SummaryMarkdown, IReadOnlyList<string> KeyPoints, IReadOnlyList<string> InterviewQuestions, IReadOnlyList<string> References);
}
