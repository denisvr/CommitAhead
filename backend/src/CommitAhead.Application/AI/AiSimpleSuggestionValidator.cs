using System.Text.Json;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Application.AI;

/// <summary>
/// Validates raw SuggestionProposals for the two AnalyzeX commands whose allowlisted
/// StructuredSuggestion commands are self-contained — no same-response cross-reference mechanism
/// needed (unlike AnalyzeJobAnalysis's AddJobRequirement/AddJobGap pair — see
/// <see cref="AiStructuredSuggestionValidator"/>). Each command's raw payload is strict-parsed,
/// field-validated, and re-serialized canonically before being persisted — never the AI's raw
/// string.
/// </summary>
internal static class AiSimpleSuggestionValidator
{
    public static IReadOnlyList<SuggestionProposal> ValidateAndBuild(
        IReadOnlyList<AiSuggestionProposal> rawProposals, IReadOnlyDictionary<StructuredSuggestionCommandType, Func<string, string>> canonicalizers)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("SuggestionProposals must not be null.");
        }

        var result = new List<SuggestionProposal>(rawProposals.Count);
        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("SuggestionProposals must not contain a null entry.");
            }

            var hasStructured = raw.CommandType is not null || raw.PayloadJson is not null;
            var hasAdvisory = raw.AdvisoryMarkdown is not null;

            if (hasStructured && hasAdvisory)
            {
                throw new AiResponseValidationException("A SuggestionProposal must not set both a structured command and AdvisoryMarkdown.");
            }

            if (hasAdvisory)
            {
                result.Add(new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion(raw.AdvisoryMarkdown!)));
                continue;
            }

            if (raw.CommandType is null || raw.PayloadJson is null)
            {
                throw new AiResponseValidationException("A SuggestionProposal must set either a structured command (CommandType and PayloadJson) or AdvisoryMarkdown.");
            }

            if (!canonicalizers.TryGetValue(raw.CommandType.Value, out var canonicalize))
            {
                throw new AiResponseValidationException($"This command does not support the '{raw.CommandType}' StructuredSuggestion.");
            }

            var canonicalPayloadJson = canonicalize(raw.PayloadJson);
            result.Add(new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(raw.CommandType.Value, canonicalPayloadJson)));
        }

        return result;
    }

    public static T Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, AiJsonOptions.Strict)
                ?? throw new AiResponseValidationException("SuggestionProposal.PayloadJson must not be null.");
        }
        catch (JsonException)
        {
            throw new AiResponseValidationException("SuggestionProposal.PayloadJson is not valid JSON for the declared CommandType.");
        }
    }
}
