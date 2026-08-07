using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Application.AI;

/// <summary>
/// Validates the two proposal kinds every AnalyzeX command shares (LinkProposal, StudyItemProposal
/// — only StructuredSuggestion validation differs per source, since the allowlisted commands
/// differ). Shared by all three AnalyzeX use cases via AnalysisCommandOrchestrator.
/// </summary>
internal static class AiProposalValidation
{
    public static IReadOnlyList<LinkProposal> ValidateLinkProposals(IReadOnlyList<AiLinkProposal> rawProposals, IReadOnlyList<StudyItemCatalogueEntry> catalogue)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("LinkProposals must not be null.");
        }

        var catalogueIds = catalogue.Select(entry => entry.Id).ToHashSet();
        var seenTargets = new HashSet<Guid>();
        var result = new List<LinkProposal>(rawProposals.Count);

        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("LinkProposals must not contain a null entry.");
            }

            if (!catalogueIds.Contains(raw.TargetStudyItemId))
            {
                throw new AiResponseValidationException("LinkProposal.TargetStudyItemId does not match a known StudyItem.");
            }

            if (!seenTargets.Add(raw.TargetStudyItemId))
            {
                throw new AiResponseValidationException("Duplicate LinkProposal.TargetStudyItemId in the same response.");
            }

            result.Add(Validate(() => new LinkProposal(Guid.NewGuid(), raw.TargetStudyItemId, raw.Weight, raw.Rationale)));
        }

        return result;
    }

    public static IReadOnlyList<StudyItemProposal> ValidateStudyItemProposals(IReadOnlyList<AiStudyItemProposal> rawProposals)
    {
        if (rawProposals is null)
        {
            throw new AiResponseValidationException("StudyItemProposals must not be null.");
        }

        var result = new List<StudyItemProposal>(rawProposals.Count);
        foreach (var raw in rawProposals)
        {
            if (raw is null)
            {
                throw new AiResponseValidationException("StudyItemProposals must not contain a null entry.");
            }

            var details = AiStudyItemDetailsParser.Parse(raw.Category, raw.DetailsJson);
            result.Add(Validate(() => new StudyItemProposal(Guid.NewGuid(), raw.Title, raw.Category, details, raw.Tags, raw.Importance)));
        }

        return result;
    }

    public static T Validate<T>(Func<T> construct)
    {
        try
        {
            return construct();
        }
        catch (DomainValidationException ex)
        {
            throw new AiResponseValidationException($"AI proposal failed validation: {ex.Message}");
        }
    }
}
