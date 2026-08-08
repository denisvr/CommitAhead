using System.Text.Json;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Json;
using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.JobAnalyses;

namespace CommitAhead.Application.AI;

/// <summary>
/// Validates a raw AiAnalysisResult's SuggestionProposals into real Domain SuggestionProposals,
/// restricted to AnalyzeJobAnalysis's own allowlist subset (AddJobRequirement, AddJobGap —
/// UpdateCVPresentationSummary/AddInterviewGap/AddInterviewLesson belong to other sources and are
/// rejected here). Resolves the same-response requirement/gap reference mechanism: an
/// AddJobRequirement proposal carries an AI-chosen, untrusted ProposalKey; an AddJobGap proposal
/// references either an existing catalogue Id or one of this response's ProposalKeys. The use case
/// — not the AI — assigns the real Guid each accepted AddJobRequirement will use, and every
/// canonical, persisted payload carries that assigned Guid, never AI-supplied text (see
/// StructuredSuggestion's doc-comment: this schema is validated again before application).
/// </summary>
internal static class AiStructuredSuggestionValidator
{
    private const int ProposalKeyMaxLength = 50;

    public static IReadOnlyList<SuggestionProposal> ValidateAndBuild(
        IReadOnlyList<AiSuggestionProposal> rawProposals, IReadOnlyList<JobRequirementCatalogueEntry> existingRequirements)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("SuggestionProposals must not be null.");
        }

        var parsed = new List<object>(rawProposals.Count);
        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("SuggestionProposals must not contain a null entry.");
            }

            parsed.Add(ParseRaw(raw));
        }

        var requirementProposals = parsed.OfType<AddJobRequirementRawPayload>().ToList();
        var proposalKeys = requirementProposals.Select(p => p.ProposalKey).ToList();
        if (proposalKeys.Distinct(StringComparer.Ordinal).Count() != proposalKeys.Count)
        {
            throw new AiResponseValidationException("Duplicate AddJobRequirement ProposalKey in the same response.");
        }

        var keyToAssignedId = requirementProposals.ToDictionary(p => p.ProposalKey, _ => Guid.NewGuid(), StringComparer.Ordinal);
        var existingRequirementIds = existingRequirements.Select(r => r.Id).ToHashSet();

        var result = new List<SuggestionProposal>(parsed.Count);
        foreach (var entry in parsed)
        {
            result.Add(entry switch
            {
                string advisoryMarkdown => new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion(advisoryMarkdown)),
                AddJobRequirementRawPayload requirement => BuildRequirementProposal(requirement, keyToAssignedId[requirement.ProposalKey]),
                AddJobGapRawPayload gap => BuildGapProposal(gap, keyToAssignedId, existingRequirementIds),
                _ => throw new InvalidOperationException("Unreachable parsed proposal kind."),
            });
        }

        return result;
    }

    private static object ParseRaw(AiSuggestionProposal raw)
    {
        var hasStructured = raw.CommandType is not null || raw.PayloadJson is not null;
        var hasAdvisory = raw.AdvisoryMarkdown is not null;

        if (hasStructured && hasAdvisory)
        {
            throw new AiResponseValidationException("A SuggestionProposal must not set both a structured command and AdvisoryMarkdown.");
        }

        if (hasAdvisory)
        {
            return raw.AdvisoryMarkdown!;
        }

        if (raw.CommandType is null || raw.PayloadJson is null)
        {
            throw new AiResponseValidationException("A SuggestionProposal must set either a structured command (CommandType and PayloadJson) or AdvisoryMarkdown.");
        }

        return raw.CommandType.Value switch
        {
            StructuredSuggestionCommandType.AddJobRequirement => ParseAddJobRequirement(raw.PayloadJson),
            StructuredSuggestionCommandType.AddJobGap => ParseAddJobGap(raw.PayloadJson),
            _ => throw new AiResponseValidationException($"AnalyzeJobAnalysis does not support the '{raw.CommandType}' command."),
        };
    }

    private static AddJobRequirementRawPayload ParseAddJobRequirement(string payloadJson)
    {
        var dto = Deserialize<AddJobRequirementRawPayload>(payloadJson);
        if (string.IsNullOrWhiteSpace(dto.ProposalKey) || dto.ProposalKey.Length > ProposalKeyMaxLength)
        {
            throw new AiResponseValidationException($"AddJobRequirement.ProposalKey must be non-blank and at most {ProposalKeyMaxLength} characters.");
        }

        // Constructed purely to reuse JobRequirement's own field validation (length caps,
        // Enum.IsDefined) instead of duplicating those limits here; discarded immediately — the
        // real, persisted Id is assigned separately by the caller, never AI-supplied.
        _ = Validate(() => new JobRequirement(Guid.NewGuid(), dto.Text, dto.Kind, dto.Priority, dto.SourceExcerpt));
        return dto;
    }

    private static AddJobGapRawPayload ParseAddJobGap(string payloadJson)
    {
        var dto = Deserialize<AddJobGapRawPayload>(payloadJson);
        var hasExisting = dto.ExistingRequirementId is not null;
        var hasProposed = dto.ProposedRequirementKey is not null;

        if (hasExisting == hasProposed)
        {
            throw new AiResponseValidationException("AddJobGap must reference exactly one of ExistingRequirementId or ProposedRequirementKey.");
        }

        // Same reasoning as ParseAddJobRequirement — RequirementId here is a placeholder purely to
        // exercise JobGap's own field validation; the real reference is resolved afterward.
        _ = Validate(() => new JobGap(Guid.NewGuid(), Guid.NewGuid(), dto.MatchLevel, dto.Severity, dto.Rationale));
        return dto;
    }

    private static SuggestionProposal BuildRequirementProposal(AddJobRequirementRawPayload requirement, Guid assignedRequirementId)
    {
        var canonical = new AddJobRequirementCanonicalPayload(assignedRequirementId, requirement.Text, requirement.Kind, requirement.Priority, requirement.SourceExcerpt);
        var payloadJson = JsonSerializer.Serialize(canonical, StrictJsonOptions.Strict);
        return new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, payloadJson));
    }

    private static SuggestionProposal BuildGapProposal(AddJobGapRawPayload gap, IReadOnlyDictionary<string, Guid> keyToAssignedId, IReadOnlySet<Guid> existingRequirementIds)
    {
        Guid resolvedRequirementId;
        if (gap.ExistingRequirementId is Guid existingId)
        {
            if (!existingRequirementIds.Contains(existingId))
            {
                throw new AiResponseValidationException("AddJobGap.ExistingRequirementId does not match a known JobRequirement.");
            }

            resolvedRequirementId = existingId;
        }
        else
        {
            if (!keyToAssignedId.TryGetValue(gap.ProposedRequirementKey!, out resolvedRequirementId))
            {
                throw new AiResponseValidationException("AddJobGap.ProposedRequirementKey does not match any AddJobRequirement proposal in this response.");
            }
        }

        var canonical = new AddJobGapCanonicalPayload(resolvedRequirementId, gap.MatchLevel, gap.Severity, gap.Rationale);
        var payloadJson = JsonSerializer.Serialize(canonical, StrictJsonOptions.Strict);
        return new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobGap, payloadJson));
    }

    private static T Deserialize<T>(string json) =>
        WrapJsonException(() => JsonSerializer.Deserialize<T>(json, StrictJsonOptions.Strict))
        ?? throw new AiResponseValidationException("SuggestionProposal.PayloadJson must not be null.");

    private static T WrapJsonException<T>(Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (JsonException)
        {
            throw new AiResponseValidationException("SuggestionProposal.PayloadJson is not valid JSON for the declared CommandType.");
        }
    }

    private static T Validate<T>(Func<T> construct)
    {
        try
        {
            return construct();
        }
        catch (DomainValidationException ex)
        {
            throw new AiResponseValidationException($"SuggestionProposal.PayloadJson failed validation: {ex.Message}");
        }
    }

    private sealed record AddJobRequirementRawPayload(string ProposalKey, string Text, JobRequirementKind Kind, JobRequirementPriority Priority, string SourceExcerpt);

    private sealed record AddJobGapRawPayload(Guid? ExistingRequirementId, string? ProposedRequirementKey, JobGapMatchLevel MatchLevel, JobGapSeverity Severity, string Rationale);
}
