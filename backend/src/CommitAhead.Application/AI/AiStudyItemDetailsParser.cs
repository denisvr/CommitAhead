using System.Text.Json;
using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AI;

/// <summary>
/// Parses an AiStudyItemProposal's DetailsJson into a real StudyItemDetails subtype. The subtype
/// is picked from Category before parsing, so a produced Details value can never mismatch its
/// Category — StudyItem's own ValidateDetails check is then trivially satisfied. Field-level
/// validation (length caps, required entries) is provided for free by each Details subtype's own
/// constructor. See <see cref="AiJsonOptions"/> for why this is a separate, deliberate parser from
/// Infrastructure's persistence converter.
/// </summary>
internal static class AiStudyItemDetailsParser
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
                _ => throw new AiResponseValidationException($"'{category}' is not a recognized StudyItemCategory."),
            };
        }
        catch (JsonException)
        {
            throw new AiResponseValidationException("StudyItemProposal.DetailsJson is not valid JSON for the proposed category.");
        }
        catch (DomainValidationException ex)
        {
            throw new AiResponseValidationException($"StudyItemProposal.DetailsJson failed validation: {ex.Message}");
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
        JsonSerializer.Deserialize<T>(json, AiJsonOptions.Strict)
        ?? throw new AiResponseValidationException("StudyItemProposal.DetailsJson must not be null.");

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
