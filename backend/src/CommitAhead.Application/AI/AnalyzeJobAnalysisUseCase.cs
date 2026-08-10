using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Identity;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.AI;

/// <summary>
/// Triggers one AnalyzeJobAnalysis AI command (ADR-0005) and produces an AnalysisDraft. The shared
/// reservation/concurrency/transaction lifecycle lives in <see cref="AnalysisCommandOrchestrator"/>
/// — this type supplies only what's JobAnalysis-specific: the <see cref="AnalyzeCommandOutcome.SourceNotFound"/>
/// check, the minimised <see cref="JobAnalysisAiInput"/>, and validating the AddJobRequirement/
/// AddJobGap allowlist (including the same-response requirement/gap reference mechanism —
/// <see cref="AiStructuredSuggestionValidator"/>).
/// </summary>
public sealed class AnalyzeJobAnalysisUseCase
{
    private readonly IJobAnalysisRepository _jobAnalysisRepository;
    private readonly IStudyItemRepository _studyItemRepository;
    private readonly IProfessionalProfileRepository _profileRepository;
    private readonly IAIProvider _aiProvider;
    private readonly IRlsSessionContext _rlsSessionContext;
    private readonly ICurrentUser _currentUser;
    private readonly AnalysisCommandOrchestrator _orchestrator;

    public AnalyzeJobAnalysisUseCase(
        IJobAnalysisRepository jobAnalysisRepository,
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IStudyItemRepository studyItemRepository,
        IProfessionalProfileRepository profileRepository,
        IAIProvider aiProvider,
        IRlsSessionContext rlsSessionContext,
        ICurrentUser currentUser,
        ILogger<AnalyzeJobAnalysisUseCase> logger)
    {
        _jobAnalysisRepository = jobAnalysisRepository;
        _studyItemRepository = studyItemRepository;
        _profileRepository = profileRepository;
        _aiProvider = aiProvider;
        _rlsSessionContext = rlsSessionContext;
        _currentUser = currentUser;
        _orchestrator = new AnalysisCommandOrchestrator(draftRepository, usageRepository, aiProvider, rlsSessionContext, logger);
    }

    public async Task<AnalyzeCommandResult> ExecuteAsync(Guid jobAnalysisId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;

        // Owner-scoped transaction of its own (ADR-0014) — the ambient RLS transaction no longer
        // wraps this action, so loading the source and building the AI input needs its own RLS
        // scope, committed before the reservation phase and the AI call that follow.
        var input = await _rlsSessionContext.RunInOwnerScopeAsync<JobAnalysisAiInput?>(
            ownerUserId,
            async ct =>
            {
                var jobAnalysis = await _jobAnalysisRepository.GetByIdAsync(ownerUserId, jobAnalysisId, ct);
                if (jobAnalysis is null)
                {
                    return null;
                }

                var existingRequirements = jobAnalysis.Requirements.Select(r => new JobRequirementCatalogueEntry(r.Id, r.Text)).ToList();
                return await BuildInputAsync(jobAnalysis, ownerUserId, existingRequirements, ct);
            },
            cancellationToken);

        if (input is null)
        {
            return new AnalyzeCommandResult(AnalyzeCommandOutcome.SourceNotFound, null);
        }

        return await _orchestrator.ExecuteAsync(
            ownerUserId,
            idempotencyKey,
            AiCommandType.AnalyzeJobAnalysis,
            EvidenceSourceType.JobAnalysis,
            jobAnalysisId,
            (limits, ct) => _aiProvider.AnalyzeJobAnalysisAsync(input, limits, ct),
            aiResult => new AnalysisDraftProposals(
                AiStructuredSuggestionValidator.ValidateAndBuild(aiResult.SuggestionProposals, input.ExistingRequirements),
                AiProposalValidation.ValidateLinkProposals(aiResult.LinkProposals, input.StudyItemCatalogue),
                AiProposalValidation.ValidateStudyItemProposals(aiResult.StudyItemProposals)),
            cancellationToken);
    }

    private async Task<JobAnalysisAiInput> BuildInputAsync(
        JobAnalysis jobAnalysis, Guid ownerUserId, IReadOnlyList<JobRequirementCatalogueEntry> existingRequirements, CancellationToken cancellationToken)
    {
        var jobPostingText = jobAnalysis.JobSource switch
        {
            PastedText pastedText => pastedText.Content,
            UploadedFile uploadedFile => uploadedFile.ExtractedText,
            _ => throw new InvalidOperationException($"Unrecognized JobSource subtype '{jobAnalysis.JobSource.GetType().Name}'."),
        };

        var profile = await _profileRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        var profileSkills = profile?.Skills.Select(s => s.DisplayName).ToList() ?? [];

        var studyItems = await _studyItemRepository.GetAllAsync(ownerUserId, cancellationToken);
        var studyItemCatalogue = studyItems
            .Where(item => item.Status == StudyItemStatus.Active)
            .Select(item => new StudyItemCatalogueEntry(item.Id, item.Title, item.Category, item.Tags))
            .ToList();

        return new JobAnalysisAiInput(jobPostingText, profileSkills, studyItemCatalogue, existingRequirements);
    }
}
