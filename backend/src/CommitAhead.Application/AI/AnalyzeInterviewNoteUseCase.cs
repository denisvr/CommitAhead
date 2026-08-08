using System.Text.Json;
using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Identity;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.Json;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.StudyItems;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.AI;

/// <summary>
/// Triggers one AnalyzeInterviewNote AI command (ADR-0005) and produces an AnalysisDraft. The
/// shared reservation/concurrency/transaction lifecycle lives in
/// <see cref="AnalysisCommandOrchestrator"/> — this type supplies only what's InterviewNote-
/// specific: the <see cref="AnalyzeCommandOutcome.SourceNotFound"/> check, the minimised
/// <see cref="InterviewNoteAiInput"/>, and validating the two allowlisted commands, AddInterviewGap
/// and AddInterviewLesson — each self-contained (a single new list entry), unlike
/// AnalyzeJobAnalysis's AddJobRequirement/AddJobGap pair, so no cross-reference mechanism is needed.
/// </summary>
public sealed class AnalyzeInterviewNoteUseCase
{
    private static readonly IReadOnlyDictionary<StructuredSuggestionCommandType, Func<string, string>> Canonicalizers =
        new Dictionary<StructuredSuggestionCommandType, Func<string, string>>
        {
            [StructuredSuggestionCommandType.AddInterviewGap] = CanonicalizeAddInterviewGap,
            [StructuredSuggestionCommandType.AddInterviewLesson] = CanonicalizeAddInterviewLesson,
        };

    private readonly IInterviewNoteRepository _interviewNoteRepository;
    private readonly IStudyItemRepository _studyItemRepository;
    private readonly IAIProvider _aiProvider;
    private readonly ICurrentUser _currentUser;
    private readonly AnalysisCommandOrchestrator _orchestrator;

    public AnalyzeInterviewNoteUseCase(
        IInterviewNoteRepository interviewNoteRepository,
        IAnalysisDraftRepository draftRepository,
        IAIUsageRecordRepository usageRepository,
        IStudyItemRepository studyItemRepository,
        IAIProvider aiProvider,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AnalyzeInterviewNoteUseCase> logger)
    {
        _interviewNoteRepository = interviewNoteRepository;
        _studyItemRepository = studyItemRepository;
        _aiProvider = aiProvider;
        _currentUser = currentUser;
        _orchestrator = new AnalysisCommandOrchestrator(draftRepository, usageRepository, aiProvider, unitOfWork, logger);
    }

    public async Task<AnalyzeCommandResult> ExecuteAsync(Guid interviewNoteId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;

        var interviewNote = await _interviewNoteRepository.GetByIdAsync(ownerUserId, interviewNoteId, cancellationToken);
        if (interviewNote is null)
        {
            return new AnalyzeCommandResult(AnalyzeCommandOutcome.SourceNotFound, null);
        }

        var input = await BuildInputAsync(interviewNote, ownerUserId, cancellationToken);

        return await _orchestrator.ExecuteAsync(
            ownerUserId,
            idempotencyKey,
            AiCommandType.AnalyzeInterviewNote,
            EvidenceSourceType.InterviewNote,
            interviewNoteId,
            (limits, ct) => _aiProvider.AnalyzeInterviewNoteAsync(input, limits, ct),
            aiResult => new AnalysisDraftProposals(
                AiSimpleSuggestionValidator.ValidateAndBuild(aiResult.SuggestionProposals, Canonicalizers),
                AiProposalValidation.ValidateLinkProposals(aiResult.LinkProposals, input.StudyItemCatalogue),
                AiProposalValidation.ValidateStudyItemProposals(aiResult.StudyItemProposals)),
            cancellationToken);
    }

    private async Task<InterviewNoteAiInput> BuildInputAsync(InterviewNote interviewNote, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var interviewRound = interviewNote.InterviewRound == InterviewRound.Other
            ? interviewNote.OtherLabel!
            : interviewNote.InterviewRound.ToString();

        var studyItems = await _studyItemRepository.GetAllAsync(ownerUserId, cancellationToken);
        var studyItemCatalogue = studyItems
            .Where(item => item.Status == StudyItemStatus.Active)
            .Select(item => new StudyItemCatalogueEntry(item.Id, item.Title, item.Category, item.Tags))
            .ToList();

        return new InterviewNoteAiInput(interviewNote.Company, interviewNote.Role, interviewRound, interviewNote.Questions, interviewNote.Gaps, interviewNote.Lessons, studyItemCatalogue);
    }

    private static string CanonicalizeAddInterviewGap(string payloadJson) => CanonicalizeEntry(payloadJson, "AddInterviewGap");

    private static string CanonicalizeAddInterviewLesson(string payloadJson) => CanonicalizeEntry(payloadJson, "AddInterviewLesson");

    private static string CanonicalizeEntry(string payloadJson, string commandName)
    {
        var dto = AiSimpleSuggestionValidator.Deserialize<InterviewNoteEntryPayload>(payloadJson);
        if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > CommitAhead.Domain.InterviewNotes.ValidationLimits.ListEntryMaxLength)
        {
            throw new AiResponseValidationException($"{commandName}.Text failed validation.");
        }

        return JsonSerializer.Serialize(dto, StrictJsonOptions.Strict);
    }
}
