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

        Assert.Equal(new HashSet<string> { "existingRequirementId", "proposedRequirementKey", "matchLevel", "severity", "rationale" }, addJobGapPayloadProperties);
        Assert.DoesNotContain("proposalKey", addJobGapPayloadProperties);
        Assert.DoesNotContain("kind", addJobGapPayloadProperties);

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

        Assert.Equal(new HashSet<string> { "summaryMarkdown", "keyPoints", "interviewQuestions", "references" }, theoryDetailsProperties);
        Assert.DoesNotContain("problemNumber", theoryDetailsProperties);
        Assert.DoesNotContain("difficulty", theoryDetailsProperties);
    }

    [Fact]
    public void BuildResponseSchema_EveryStudyItemVariant_PairsItsOwnCategoryWithItsOwnDetailsShape_NeverCrossed()
    {
        var schema = AnthropicStructuredOutputSchema.BuildResponseSchema([StructuredSuggestionCommandType.AddJobRequirement]);
        var studyItemVariants = GetItemAnyOfVariants(schema, "studyItemProposals");

        var expectedDetailsPropertiesByCategory = new Dictionary<string, HashSet<string>>
        {
            ["Theory"] = new() { "summaryMarkdown", "keyPoints", "interviewQuestions", "references" },
            ["LeetCode"] = new() { "problemNumber", "url", "difficulty", "patterns", "expectedTimeComplexity", "expectedSpaceComplexity", "approachMarkdown", "csharpSolution" },
            ["SystemDesign"] = new() { "promptMarkdown", "clarifyingQuestions", "functionalRequirements", "nonFunctionalRequirements", "evaluationChecklist", "referenceSolutionMarkdown" },
            ["Behavioral"] = new() { "competencies", "questionVariants", "situation", "task", "action", "result", "reflection" },
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
