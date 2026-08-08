using System.Text.Json;
using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Json;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.StudyItems;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.AI;

/// <summary>
/// Triggers one AnalyzeCVPresentation AI command (ADR-0005) and produces an AnalysisDraft. The
/// shared reservation/concurrency/transaction lifecycle lives in
/// <see cref="AnalysisCommandOrchestrator"/> — this type supplies only what's CVPresentation-
/// specific: the <see cref="AnalyzeCommandOutcome.SourceNotFound"/> check, the minimised
/// <see cref="CVPresentationAiInput"/> (selected canonical entries flattened into short highlight
/// strings — exact formatting is deliberately simple; a richer projection is a later concern, not
/// this slice's), and validating the single allowlisted command, UpdateCVPresentationSummary.
/// </summary>
public sealed class AnalyzeCVPresentationUseCase
{
    private static readonly IReadOnlyDictionary<StructuredSuggestionCommandType, Func<string, string>> Canonicalizers =
        new Dictionary<StructuredSuggestionCommandType, Func<string, string>>
        {
            [StructuredSuggestionCommandType.UpdateCVPresentationSummary] = CanonicalizeUpdateSummary,
        };

    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly IProfessionalProfileRepository _profileRepository;
    private readonly IStudyItemRepository _studyItemRepository;
    private readonly IAIProvider _aiProvider;
    private readonly ICurrentUser _currentUser;
    private readonly AnalysisCommandOrchestrator _orchestrator;

    public AnalyzeCVPresentationUseCase(
        ICVPresentationRepository cvPresentationRepository,
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IProfessionalProfileRepository profileRepository,
        IStudyItemRepository studyItemRepository,
        IAIProvider aiProvider,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AnalyzeCVPresentationUseCase> logger)
    {
        _cvPresentationRepository = cvPresentationRepository;
        _profileRepository = profileRepository;
        _studyItemRepository = studyItemRepository;
        _aiProvider = aiProvider;
        _currentUser = currentUser;
        _orchestrator = new AnalysisCommandOrchestrator(draftRepository, usageRepository, aiProvider, unitOfWork, logger);
    }

    public async Task<AnalyzeCommandResult> ExecuteAsync(Guid cvPresentationId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;

        var cvPresentation = await _cvPresentationRepository.GetByIdAsync(ownerUserId, cvPresentationId, cancellationToken);
        if (cvPresentation is null)
        {
            return new AnalyzeCommandResult(AnalyzeCommandOutcome.SourceNotFound, null);
        }

        var input = await BuildInputAsync(cvPresentation, ownerUserId, cancellationToken);

        return await _orchestrator.ExecuteAsync(
            ownerUserId,
            idempotencyKey,
            AiCommandType.AnalyzeCVPresentation,
            EvidenceSourceType.CVPresentation,
            cvPresentationId,
            (limits, ct) => _aiProvider.AnalyzeCVPresentationAsync(input, limits, ct),
            aiResult => new AnalysisDraftProposals(
                AiSimpleSuggestionValidator.ValidateAndBuild(aiResult.SuggestionProposals, Canonicalizers),
                AiProposalValidation.ValidateLinkProposals(aiResult.LinkProposals, input.StudyItemCatalogue),
                AiProposalValidation.ValidateStudyItemProposals(aiResult.StudyItemProposals)),
            cancellationToken);
    }

    private async Task<CVPresentationAiInput> BuildInputAsync(CVPresentation cvPresentation, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("A CVPresentation must have a ProfessionalProfile.");

        var experienceById = profile.Experience.ToDictionary(e => e.Id);
        var experienceHighlights = cvPresentation.ExperienceSelections
            .Where(experienceById.ContainsKey)
            .Select(id => $"{experienceById[id].Role} at {experienceById[id].Company}")
            .ToList();

        var educationById = profile.Education.ToDictionary(e => e.Id);
        var educationHighlights = cvPresentation.EducationSelections
            .Where(educationById.ContainsKey)
            .Select(id => $"{educationById[id].Degree}, {educationById[id].Institution}")
            .ToList();

        var skillById = profile.Skills.ToDictionary(s => s.Id);
        var skillNames = cvPresentation.SkillSelections
            .Where(skillById.ContainsKey)
            .Select(id => skillById[id].DisplayName)
            .ToList();

        var studyItems = await _studyItemRepository.GetAllAsync(ownerUserId, cancellationToken);
        var studyItemCatalogue = studyItems
            .Where(item => item.Status == StudyItemStatus.Active)
            .Select(item => new StudyItemCatalogueEntry(item.Id, item.Title, item.Category, item.Tags))
            .ToList();

        var summaryMarkdown = cvPresentation.SummaryOverrideMarkdown ?? profile.SummaryMarkdown;

        return new CVPresentationAiInput(summaryMarkdown, experienceHighlights, educationHighlights, skillNames, studyItemCatalogue);
    }

    private static string CanonicalizeUpdateSummary(string payloadJson)
    {
        var dto = AiSimpleSuggestionValidator.Deserialize<UpdateCVPresentationSummaryPayload>(payloadJson);
        if (string.IsNullOrWhiteSpace(dto.SummaryMarkdown) || dto.SummaryMarkdown.Length > CommitAhead.Domain.ProfessionalProfiles.ValidationLimits.MarkdownMaxLength)
        {
            throw new AiResponseValidationException("UpdateCVPresentationSummary.SummaryMarkdown failed validation.");
        }

        return JsonSerializer.Serialize(dto, StrictJsonOptions.Strict);
    }
}
