namespace CommitAhead.Application.AI;

/// <summary>
/// The compact existing-JobRequirement catalogue AnalyzeJobAnalysis sends alongside the job
/// posting text — mirrors <see cref="StudyItemCatalogueEntry"/>'s "just enough to reference"
/// precedent. Lets an AddJobGap proposal reference an existing requirement by a real Id (empty on
/// a fresh JobAnalysis with no requirements yet — see AnalyzeJobAnalysisUseCase's same-response
/// ProposalKey mechanism for a gap against a requirement proposed in the same response).
/// </summary>
public sealed record JobRequirementCatalogueEntry(Guid Id, string Text);
