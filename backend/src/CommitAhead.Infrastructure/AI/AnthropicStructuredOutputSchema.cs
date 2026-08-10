using System.Text.Json.Nodes;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Infrastructure.AI;

/// <summary>
/// Builds the strict JSON Schema every AnthropicAIProvider call sends via Structured Outputs —
/// mirrors AiAnalysisResult's three list shapes (AiSuggestionProposal/AiLinkProposal/
/// AiStudyItemProposal) field-for-field, but with the polymorphic payload/details fields as
/// concrete object variants (one per allowed StructuredSuggestionCommandType, one per
/// StudyItemCategory) rather than a permissive object — every object declares
/// additionalProperties:false and every property required (with nullable types made explicit),
/// per ADR-0019. Each variant is a whole-object union member (Anthropic's anyOf, not oneOf) that
/// fixes its discriminator (commandType/category) together with its own concrete payload/details
/// shape, so a commandType/category value can never be paired with a mismatched payload shape —
/// e.g. category:"Theory" can never validate alongside LeetCode-only fields. This schema is
/// descriptive scaffolding for the provider only; every existing Application-layer validator
/// remains the authoritative check on the resulting data.
/// </summary>
internal static class AnthropicStructuredOutputSchema
{
    public static JsonObject BuildResponseSchema(IReadOnlyList<StructuredSuggestionCommandType> allowedCommands)
    {
        return StrictObject(
            ("suggestionProposals", ArrayOf(SuggestionProposalSchema(allowedCommands))),
            ("linkProposals", ArrayOf(LinkProposalSchema())),
            ("studyItemProposals", ArrayOf(StudyItemProposalSchema())));
    }

    private static JsonObject SuggestionProposalSchema(IReadOnlyList<StructuredSuggestionCommandType> allowedCommands)
    {
        var variants = new JsonNode[] { AdvisorySuggestionVariant() }
            .Concat(allowedCommands.Select(CommandSuggestionVariant))
            .ToArray();

        return AnyOf(variants);
    }

    private static JsonObject AdvisorySuggestionVariant() => StrictObject(
        ("commandType", NullSchema()),
        ("payload", NullSchema()),
        ("advisoryMarkdown", StringSchema()));

    private static JsonObject CommandSuggestionVariant(StructuredSuggestionCommandType commandType) => StrictObject(
        ("commandType", EnumSchema([commandType.ToString()])),
        ("payload", PayloadSchemaFor(commandType)),
        ("advisoryMarkdown", NullSchema()));

    private static JsonObject PayloadSchemaFor(StructuredSuggestionCommandType commandType) => commandType switch
    {
        StructuredSuggestionCommandType.AddJobRequirement => StrictObject(
            ("proposalKey", StringSchema()),
            ("text", StringSchema()),
            ("kind", EnumSchema(Enum.GetNames<JobRequirementKind>())),
            ("priority", EnumSchema(Enum.GetNames<JobRequirementPriority>())),
            ("sourceExcerpt", StringSchema())),
        StructuredSuggestionCommandType.AddJobGap => StrictObject(
            ("existingRequirementId", Nullable(StringSchema())),
            ("proposedRequirementKey", Nullable(StringSchema())),
            ("matchLevel", EnumSchema(Enum.GetNames<JobGapMatchLevel>())),
            ("severity", EnumSchema(Enum.GetNames<JobGapSeverity>())),
            ("rationale", StringSchema())),
        StructuredSuggestionCommandType.UpdateCVPresentationSummary => StrictObject(
            ("summaryMarkdown", Nullable(StringSchema()))),
        StructuredSuggestionCommandType.AddInterviewGap or StructuredSuggestionCommandType.AddInterviewLesson => StrictObject(
            ("text", StringSchema())),
        _ => throw new InvalidOperationException($"No structured-output payload schema defined for '{commandType}'."),
    };

    private static JsonObject LinkProposalSchema() => StrictObject(
        ("targetStudyItemId", StringSchema()),
        ("weight", NumberSchema()),
        ("rationale", StringSchema()));

    private static JsonObject StudyItemProposalSchema() => AnyOf(
        StudyItemVariant(StudyItemCategory.Theory, TheoryDetailsSchema()),
        StudyItemVariant(StudyItemCategory.LeetCode, LeetCodeDetailsSchema()),
        StudyItemVariant(StudyItemCategory.SystemDesign, SystemDesignDetailsSchema()),
        StudyItemVariant(StudyItemCategory.Behavioral, BehavioralDetailsSchema()));

    private static JsonObject StudyItemVariant(StudyItemCategory category, JsonObject detailsSchema) => StrictObject(
        ("title", StringSchema()),
        ("category", EnumSchema([category.ToString()])),
        ("details", detailsSchema),
        ("tags", ArrayOf(StringSchema())),
        ("importance", IntegerSchema()));

    private static JsonObject TheoryDetailsSchema() => StrictObject(
        ("summaryMarkdown", StringSchema()),
        ("keyPoints", ArrayOf(StringSchema())),
        ("interviewQuestions", ArrayOf(StringSchema())),
        ("references", ArrayOf(StringSchema())));

    private static JsonObject LeetCodeDetailsSchema() => StrictObject(
        ("problemNumber", Nullable(IntegerSchema())),
        ("url", Nullable(StringSchema())),
        ("difficulty", EnumSchema(Enum.GetNames<Difficulty>())),
        ("patterns", ArrayOf(StringSchema())),
        ("expectedTimeComplexity", StringSchema()),
        ("expectedSpaceComplexity", StringSchema()),
        ("approachMarkdown", StringSchema()),
        ("csharpSolution", Nullable(StringSchema())));

    private static JsonObject SystemDesignDetailsSchema() => StrictObject(
        ("promptMarkdown", StringSchema()),
        ("clarifyingQuestions", ArrayOf(StringSchema())),
        ("functionalRequirements", ArrayOf(StringSchema())),
        ("nonFunctionalRequirements", ArrayOf(StringSchema())),
        ("evaluationChecklist", ArrayOf(StringSchema())),
        ("referenceSolutionMarkdown", StringSchema()));

    private static JsonObject BehavioralDetailsSchema() => StrictObject(
        ("competencies", ArrayOf(StringSchema())),
        ("questionVariants", ArrayOf(StringSchema())),
        ("situation", StringSchema()),
        ("task", StringSchema()),
        ("action", StringSchema()),
        ("result", StringSchema()),
        ("reflection", Nullable(StringSchema())));

    private static JsonObject StrictObject(params (string Name, JsonNode Schema)[] properties)
    {
        var propertiesNode = new JsonObject();
        foreach (var (name, schema) in properties)
        {
            propertiesNode[name] = schema;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertiesNode,
            ["required"] = new JsonArray(properties.Select(p => JsonValue.Create(p.Name)).ToArray<JsonNode?>()),
            ["additionalProperties"] = false,
        };
    }

    private static JsonObject ArrayOf(JsonNode itemSchema) => new()
    {
        ["type"] = "array",
        ["items"] = itemSchema,
    };

    private static JsonObject StringSchema() => new() { ["type"] = "string" };

    private static JsonObject NumberSchema() => new() { ["type"] = "number" };

    private static JsonObject IntegerSchema() => new() { ["type"] = "integer" };

    private static JsonObject NullSchema() => new() { ["type"] = "null" };

    private static JsonObject EnumSchema(IEnumerable<string> values) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray(values.Select(v => JsonValue.Create(v)).ToArray<JsonNode?>()),
    };

    /// <summary>Represents an explicitly nullable field as ["null", &lt;schema's own type&gt;] per ADR-0019's "nullable fields represented explicitly," never by simply omitting the property.</summary>
    private static JsonObject Nullable(JsonObject schema)
    {
        var clone = (JsonObject)schema.DeepClone();
        var baseType = clone["type"]?.GetValue<string>() ?? throw new InvalidOperationException("Nullable() requires a schema with a scalar 'type'.");
        clone["type"] = new JsonArray(baseType, "null");
        return clone;
    }

    /// <summary>Anthropic's Structured Outputs supports anyOf (not oneOf) for whole-object discriminated unions — each variant here fixes its discriminator together with its own concrete payload/details shape.</summary>
    private static JsonObject AnyOf(params JsonNode[] schemas) => new()
    {
        ["anyOf"] = new JsonArray(schemas),
    };
}
