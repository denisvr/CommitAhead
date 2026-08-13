using System.Text.Json.Nodes;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.AI;

namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>
/// Corrective-pass coverage: the schema must bind each discriminator (commandType/category) to its
/// own concrete payload/details shape, not just declare both as independent sibling fields — a
/// response pairing category:"Theory" with LeetCode-only fields must be structurally unrepresentable,
/// not merely rejected later by the Application-layer validators.
/// </summary>
public sealed class AnthropicStructuredOutputSchemaTests
{
    [Fact]
    public void BuildResponseSchema_NeverUsesOneOf_OnlyAnyOfForWholeObjectUnions()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema(
            [StructuredSuggestionCommandType.AddJobRequirement, StructuredSuggestionCommandType.AddJobGap]);

        Assert.False(ContainsKeyAnywhere(schema, "oneOf"), "The schema must not use oneOf — Anthropic Structured Outputs supports anyOf for unions.");
        Assert.True(ContainsKeyAnywhere(schema, "anyOf"), "Expected at least one anyOf union (suggestionProposals and studyItemProposals item schemas).");
    }

    [Fact]
    public void BuildResponseSchema_TheAddJobGapVariant_PairsItsCommandTypeWithItsOwnPayloadShape_NeverAddJobRequirements()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema(
            [StructuredSuggestionCommandType.AddJobRequirement, StructuredSuggestionCommandType.AddJobGap]);

        var suggestionVariants = GetItemAnyOfVariants(schema, "suggestionProposals");
        var addJobGapVariant = suggestionVariants.Single(v => VariantEnumValue(v, "commandType") == "AddJobGap");
        var addJobRequirementVariant = suggestionVariants.Single(v => VariantEnumValue(v, "commandType") == "AddJobRequirement");

        var addJobGapPayloadProperties = PropertyNames((JsonObject)addJobGapVariant["properties"]!["payload"]!);
        var addJobRequirementPayloadProperties = PropertyNames((JsonObject)addJobRequirementVariant["properties"]!["payload"]!);

        Assert.Equal(new HashSet<string> { "ExistingRequirementId", "ProposedRequirementKey", "MatchLevel", "Severity", "Rationale" }, addJobGapPayloadProperties);
        Assert.DoesNotContain("ProposalKey", addJobGapPayloadProperties);
        Assert.DoesNotContain("Kind", addJobGapPayloadProperties);

        // Sanity check the sibling variant is genuinely different, proving the two are not accidentally identical.
        Assert.NotEqual(addJobGapPayloadProperties, addJobRequirementPayloadProperties);
    }

    [Fact]
    public void BuildResponseSchema_TheAdvisoryVariant_HasNullCommandTypeAndPayload_AndStringAdvisoryMarkdown()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([StructuredSuggestionCommandType.AddJobRequirement]);

        var suggestionVariants = GetItemAnyOfVariants(schema, "suggestionProposals");
        var advisoryVariant = suggestionVariants.Single(v => v["properties"]!["commandType"]!["type"]!.GetValue<string>() == "null");

        Assert.Equal("null", advisoryVariant["properties"]!["payload"]!["type"]!.GetValue<string>());
        Assert.Equal("string", advisoryVariant["properties"]!["advisoryMarkdown"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void BuildResponseSchema_TheTheoryVariant_HasExactlyTheoryDetails_NeverLeetCodeOnlyFields()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([StructuredSuggestionCommandType.AddJobRequirement]);

        var studyItemVariants = GetItemAnyOfVariants(schema, "studyItemProposals");
        var theoryVariant = studyItemVariants.Single(v => VariantEnumValue(v, "category") == "Theory");
        var theoryDetailsProperties = PropertyNames((JsonObject)theoryVariant["properties"]!["details"]!);

        Assert.Equal(new HashSet<string> { "SummaryMarkdown", "KeyPoints", "InterviewQuestions", "References" }, theoryDetailsProperties);
        Assert.DoesNotContain("ProblemNumber", theoryDetailsProperties);
        Assert.DoesNotContain("Difficulty", theoryDetailsProperties);
    }

    [Fact]
    public void BuildResponseSchema_EveryStudyItemVariant_PairsItsOwnCategoryWithItsOwnDetailsShape_NeverCrossed()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([StructuredSuggestionCommandType.AddJobRequirement]);
        var studyItemVariants = GetItemAnyOfVariants(schema, "studyItemProposals");

        var expectedDetailsPropertiesByCategory = new Dictionary<string, HashSet<string>>
        {
            ["Theory"] = new() { "SummaryMarkdown", "KeyPoints", "InterviewQuestions", "References" },
            ["LeetCode"] = new() { "ProblemNumber", "Url", "Difficulty", "Patterns", "ExpectedTimeComplexity", "ExpectedSpaceComplexity", "ApproachMarkdown", "CSharpSolution" },
            ["SystemDesign"] = new() { "PromptMarkdown", "ClarifyingQuestions", "FunctionalRequirements", "NonFunctionalRequirements", "EvaluationChecklist", "ReferenceSolutionMarkdown" },
            ["Behavioral"] = new() { "Competencies", "QuestionVariants", "Situation", "Task", "Action", "Result", "Reflection" },
        };

        Assert.Equal(Enum.GetNames<StudyItemCategory>().Length, studyItemVariants.Count);

        foreach (var variant in studyItemVariants)
        {
            var category = VariantEnumValue(variant, "category")!;
            var detailsProperties = PropertyNames((JsonObject)variant["properties"]!["details"]!);
            Assert.Equal(expectedDetailsPropertiesByCategory[category], detailsProperties);

            foreach (var (otherCategory, otherProperties) in expectedDetailsPropertiesByCategory)
            {
                if (otherCategory == category)
                {
                    continue;
                }

                // No variant may pair its category with another category's exclusive fields —
                // this is the exact Theory+LeetCode combination the corrective pass targets.
                var exclusiveToOther = otherProperties.Except(expectedDetailsPropertiesByCategory[category]);
                Assert.Empty(detailsProperties.Intersect(exclusiveToOther));
            }
        }
    }

    /// <summary>
    /// Regression coverage for the Journey 3 casing defect: the outer envelope every
    /// AnthropicAIProvider wire DTO already expects (commandType/payload/advisoryMarkdown,
    /// targetStudyItemId/weight/rationale, title/category/details/tags/importance) is camelCase,
    /// but everything *inside* a payload/details object must be the same canonical PascalCase
    /// every existing consumer (AiStructuredSuggestionValidator, AiSimpleSuggestionValidator,
    /// StudyItemDetailsJsonParser, the frontend's payloadFields.ts) already uses — never the other
    /// way around, and never uniformly one casing throughout.
    /// </summary>
    [Theory]
    [InlineData(StructuredSuggestionCommandType.AddJobRequirement, "ProposalKey")]
    [InlineData(StructuredSuggestionCommandType.AddJobGap, "MatchLevel")]
    [InlineData(StructuredSuggestionCommandType.UpdateCVPresentationSummary, "SummaryMarkdown")]
    [InlineData(StructuredSuggestionCommandType.AddInterviewGap, "Text")]
    [InlineData(StructuredSuggestionCommandType.AddInterviewLesson, "Text")]
    public void BuildResponseSchema_EveryAllowedCommand_HasACamelCaseEnvelopeButPascalCasePayload(StructuredSuggestionCommandType commandType, string expectedPayloadProperty)
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([commandType]);

        // Envelope: unaffected by this fix, still camelCase — matches AnthropicAIProvider's
        // [JsonPropertyName]-annotated wire DTOs exactly.
        var suggestionProposalsSchema = (JsonObject)schema["properties"]!;
        Assert.True(suggestionProposalsSchema.ContainsKey("suggestionProposals"));
        Assert.True(suggestionProposalsSchema.ContainsKey("linkProposals"));
        Assert.True(suggestionProposalsSchema.ContainsKey("studyItemProposals"));

        var variants = GetItemAnyOfVariants(schema, "suggestionProposals");
        var variant = variants.Single(v => VariantEnumValue(v, "commandType") == commandType.ToString());
        var variantProperties = (JsonObject)variant["properties"]!;
        Assert.True(variantProperties.ContainsKey("commandType"));
        Assert.True(variantProperties.ContainsKey("payload"));
        Assert.True(variantProperties.ContainsKey("advisoryMarkdown"));

        // Payload contents: the fix — canonical PascalCase, never the envelope's camelCase.
        var payloadProperties = PropertyNames((JsonObject)variantProperties["payload"]!);
        Assert.Contains(expectedPayloadProperty, payloadProperties);
        Assert.DoesNotContain(char.ToLowerInvariant(expectedPayloadProperty[0]) + expectedPayloadProperty[1..], payloadProperties);
    }

    [Theory]
    [InlineData("Theory", "SummaryMarkdown")]
    [InlineData("LeetCode", "ProblemNumber")]
    [InlineData("SystemDesign", "PromptMarkdown")]
    [InlineData("Behavioral", "Situation")]
    public void BuildResponseSchema_EveryStudyItemCategory_HasACamelCaseEnvelopeButPascalCaseDetails(string category, string expectedDetailsProperty)
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([StructuredSuggestionCommandType.AddJobRequirement]);

        var variants = GetItemAnyOfVariants(schema, "studyItemProposals");
        var variant = variants.Single(v => VariantEnumValue(v, "category") == category);
        var variantProperties = (JsonObject)variant["properties"]!;
        Assert.True(variantProperties.ContainsKey("title"));
        Assert.True(variantProperties.ContainsKey("category"));
        Assert.True(variantProperties.ContainsKey("details"));
        Assert.True(variantProperties.ContainsKey("tags"));
        Assert.True(variantProperties.ContainsKey("importance"));

        var detailsProperties = PropertyNames((JsonObject)variantProperties["details"]!);
        Assert.Contains(expectedDetailsProperty, detailsProperties);
        Assert.DoesNotContain(char.ToLowerInvariant(expectedDetailsProperty[0]) + expectedDetailsProperty[1..], detailsProperties);
    }

    private static List<JsonObject> GetItemAnyOfVariants(JsonObject schema, string arrayPropertyName)
    {
        var itemSchema = (JsonObject)schema["properties"]![arrayPropertyName]!["items"]!;
        var anyOf = (JsonArray)itemSchema["anyOf"]!;
        return anyOf.Select(node => (JsonObject)node!).ToList();
    }

    private static string? VariantEnumValue(JsonObject variant, string propertyName)
    {
        var propertySchema = variant["properties"]![propertyName]!;
        var enumValues = propertySchema["enum"] as JsonArray;
        return enumValues?.Single()?.GetValue<string>();
    }

    private static HashSet<string> PropertyNames(JsonObject objectSchema) =>
        ((JsonObject)objectSchema["properties"]!).Select(p => p.Key).ToHashSet();

    private static bool ContainsKeyAnywhere(JsonNode? node, string key)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey(key))
                {
                    return true;
                }

                return obj.Any(p => ContainsKeyAnywhere(p.Value, key));
            case JsonArray array:
                return array.Any(item => ContainsKeyAnywhere(item, key));
            default:
                return false;
        }
    }
}
