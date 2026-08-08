using System.Text.Json;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Application.Identity;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Json;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>
/// Applies one Pending AnalysisDraft (ADR-0005/ADR-0004): given exactly one decision per proposal
/// (with a complete final payload for every accepted actionable proposal), locks the draft,
/// validates everything, then atomically applies every accepted effect and marks the draft Applied.
///
/// Concurrency: <see cref="IAnalysisDraftRepository.GetByIdForUpdateAsync"/> row-locks the draft
/// for the transaction's duration — a second concurrent apply of the same draft blocks until the
/// first commits, then observes <see cref="AnalysisDraftStatus.Applied"/> and returns
/// <see cref="ApplyAnalysisDraftOutcome.DraftNotPending"/> without any effect.
///
/// Resolve-then-mutate (avoids partial in-memory mutation on a later failure): every decision is
/// parsed, validated, and resolved into an in-memory effect first; only once every decision
/// resolves without error does a second pass perform the actual mutations (JobRequirement/JobGap
/// additions to a JobAnalysis are ordered before any JobGap that references one from the same
/// batch — JobAnalysis.AddGap's own existing check is what actually rejects a gap whose sibling
/// requirement was not accepted).
///
/// Every internal failure (bad JSON, bad enum, Domain validation, an already-existing or target-
/// missing EvidenceLink) is wrapped as <see cref="ApplyAnalysisDraftValidationException"/> — never
/// a raw <c>JsonException</c>/<c>DomainValidationException</c>/<c>StudyItemDetailsPayloadException</c>.
/// The one exception this doesn't cover, <see cref="EvidenceLinkConflictException"/> (the database's
/// own last-resort duplicate guard beneath this use case's own pre-check), is deliberately caught
/// *outside* <see cref="IUnitOfWork.ExecuteInTransactionAsync{T}"/> — after rollback and
/// <c>ChangeTracker.Clear()</c> have already run — and translated the same way. Every other
/// exception (an unexpected database failure, caller cancellation) propagates unchanged.
/// </summary>
public sealed class ApplyAnalysisDraftUseCase
{
    private static readonly IReadOnlyDictionary<EvidenceSourceType, IReadOnlySet<StructuredSuggestionCommandType>> AllowedCommandsBySource =
        new Dictionary<EvidenceSourceType, IReadOnlySet<StructuredSuggestionCommandType>>
        {
            [EvidenceSourceType.JobAnalysis] = new HashSet<StructuredSuggestionCommandType> { StructuredSuggestionCommandType.AddJobRequirement, StructuredSuggestionCommandType.AddJobGap },
            [EvidenceSourceType.CVPresentation] = new HashSet<StructuredSuggestionCommandType> { StructuredSuggestionCommandType.UpdateCVPresentationSummary },
            [EvidenceSourceType.InterviewNote] = new HashSet<StructuredSuggestionCommandType> { StructuredSuggestionCommandType.AddInterviewGap, StructuredSuggestionCommandType.AddInterviewLesson },
        };

    private readonly IAnalysisDraftRepository _draftRepository;
    private readonly IJobAnalysisRepository _jobAnalysisRepository;
    private readonly ICVPresentationRepository _cvPresentationRepository;
    private readonly IInterviewNoteRepository _interviewNoteRepository;
    private readonly IStudyItemRepository _studyItemRepository;
    private readonly IEvidenceLinkRepository _evidenceLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ApplyAnalysisDraftUseCase(
        IAnalysisDraftRepository draftRepository,
        IJobAnalysisRepository jobAnalysisRepository,
        ICVPresentationRepository cvPresentationRepository,
        IInterviewNoteRepository interviewNoteRepository,
        IStudyItemRepository studyItemRepository,
        IEvidenceLinkRepository evidenceLinkRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _draftRepository = draftRepository;
        _jobAnalysisRepository = jobAnalysisRepository;
        _cvPresentationRepository = cvPresentationRepository;
        _interviewNoteRepository = interviewNoteRepository;
        _studyItemRepository = studyItemRepository;
        _evidenceLinkRepository = evidenceLinkRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ApplyAnalysisDraftOutcome> ExecuteAsync(
        Guid draftId,
        IReadOnlyList<SuggestionProposalDecision> suggestionDecisions,
        IReadOnlyList<LinkProposalDecision> linkDecisions,
        IReadOnlyList<StudyItemProposalDecision> studyItemDecisions,
        CancellationToken cancellationToken)
    {
        var ownerUserId = _currentUser.UserId;

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                ct => ApplyWithinTransactionAsync(ownerUserId, draftId, suggestionDecisions, linkDecisions, studyItemDecisions, ct),
                cancellationToken);
        }
        catch (EvidenceLinkConflictException)
        {
            throw new ApplyAnalysisDraftValidationException("An EvidenceLink for this source and target already exists.");
        }
    }

    private async Task<ApplyAnalysisDraftOutcome> ApplyWithinTransactionAsync(
        Guid ownerUserId,
        Guid draftId,
        IReadOnlyList<SuggestionProposalDecision> suggestionDecisions,
        IReadOnlyList<LinkProposalDecision> linkDecisions,
        IReadOnlyList<StudyItemProposalDecision> studyItemDecisions,
        CancellationToken ct)
    {
        var draft = await _draftRepository.GetByIdForUpdateAsync(ownerUserId, draftId, ct);
        if (draft is null)
        {
            return ApplyAnalysisDraftOutcome.DraftNotFound;
        }

        if (draft.Status != AnalysisDraftStatus.Pending)
        {
            return ApplyAnalysisDraftOutcome.DraftNotPending;
        }

        ValidateCoverage(draft.SuggestionProposals, p => p.Id, suggestionDecisions, d => d.ProposalId, "SuggestionProposal");
        ValidateCoverage(draft.LinkProposals, p => p.Id, linkDecisions, d => d.ProposalId, "LinkProposal");
        ValidateCoverage(draft.StudyItemProposals, p => p.Id, studyItemDecisions, d => d.ProposalId, "StudyItemProposal");

        if (!AllowedCommandsBySource.TryGetValue(draft.SourceType, out var allowedCommands))
        {
            throw new ApplyAnalysisDraftValidationException($"'{draft.SourceType}' is not a supported AnalysisDraft source type.");
        }

        JobAnalysis? jobAnalysis = null;
        CVPresentation? cvPresentation = null;
        InterviewNote? interviewNote = null;

        switch (draft.SourceType)
        {
            case EvidenceSourceType.JobAnalysis:
                jobAnalysis = await _jobAnalysisRepository.GetByIdAsync(ownerUserId, draft.SourceId, ct);
                break;
            case EvidenceSourceType.CVPresentation:
                cvPresentation = await _cvPresentationRepository.GetByIdAsync(ownerUserId, draft.SourceId, ct);
                break;
            case EvidenceSourceType.InterviewNote:
                interviewNote = await _interviewNoteRepository.GetByIdAsync(ownerUserId, draft.SourceId, ct);
                break;
        }

        if (jobAnalysis is null && cvPresentation is null && interviewNote is null)
        {
            return ApplyAnalysisDraftOutcome.SourceNotFound;
        }

        // ---- Resolve: validate and construct every effect; nothing is mutated yet. ----
        var resolvedSuggestions = ResolveSuggestionDecisions(suggestionDecisions, draft, allowedCommands, jobAnalysis);
        var resolvedLinks = await ResolveLinkDecisionsAsync(linkDecisions, draft, ownerUserId, ct);
        var resolvedStudyItems = ResolveStudyItemDecisions(studyItemDecisions, ownerUserId);

        var utcNow = DateTime.UtcNow;

        // ---- Mutate: only now that every decision has resolved successfully. ----

        // JobRequirements before JobGaps — a gap referencing a same-batch requirement needs it to
        // already be on the aggregate (JobAnalysis.AddGap's own existing check enforces this).
        foreach (var effect in resolvedSuggestions.OfType<ResolvedJobRequirementEffect>())
        {
            jobAnalysis!.AddRequirement(effect.JobRequirement, utcNow);
        }

        foreach (var effect in resolvedSuggestions.OfType<ResolvedJobGapEffect>())
        {
            jobAnalysis!.AddGap(effect.JobGap, utcNow);
        }

        foreach (var effect in resolvedSuggestions.OfType<ResolvedCVSummaryEffect>())
        {
            cvPresentation!.Update(
                cvPresentation.Label, cvPresentation.TargetMarket, cvPresentation.TargetRole, cvPresentation.Locale, cvPresentation.TemplateKey,
                effect.SummaryMarkdownToSet, cvPresentation.IncludePhoto, cvPresentation.IncludeEmail, cvPresentation.IncludePhone, cvPresentation.IncludeAddress,
                cvPresentation.DateFormat, cvPresentation.PageLimit, utcNow);
            effect.AppliedSummaryMarkdown = cvPresentation.SummaryOverrideMarkdown;
        }

        var interviewEntryEffects = resolvedSuggestions.OfType<ResolvedInterviewEntryEffect>().ToList();
        if (interviewEntryEffects.Count > 0)
        {
            var existingGapCount = interviewNote!.Gaps.Count;
            var existingLessonCount = interviewNote.Lessons.Count;
            var newGaps = interviewEntryEffects.Where(e => e.CommandType == StructuredSuggestionCommandType.AddInterviewGap).Select(e => e.TextToSet).ToList();
            var newLessons = interviewEntryEffects.Where(e => e.CommandType == StructuredSuggestionCommandType.AddInterviewLesson).Select(e => e.TextToSet).ToList();

            interviewNote.Update(
                interviewNote.Company, interviewNote.Role, interviewNote.InterviewRound, interviewNote.SequenceNumber, interviewNote.OtherLabel, interviewNote.Date,
                interviewNote.Questions, interviewNote.Gaps.Concat(newGaps), interviewNote.Lessons.Concat(newLessons), interviewNote.JobAnalysisId, utcNow);

            var appliedGaps = interviewNote.Gaps.Skip(existingGapCount).ToList();
            var appliedLessons = interviewNote.Lessons.Skip(existingLessonCount).ToList();
            var gapIndex = 0;
            var lessonIndex = 0;
            foreach (var effect in interviewEntryEffects)
            {
                effect.AppliedText = effect.CommandType == StructuredSuggestionCommandType.AddInterviewGap
                    ? appliedGaps[gapIndex++]
                    : appliedLessons[lessonIndex++];
            }
        }

        foreach (var decision in suggestionDecisions)
        {
            var proposal = draft.SuggestionProposals.Single(p => p.Id == decision.ProposalId);
            if (!decision.Accepted)
            {
                proposal.Reject();
                continue;
            }

            var effect = resolvedSuggestions.Single(e => e.ProposalId == decision.ProposalId);
            proposal.Accept(effect.BuildAcceptedPayload());
        }

        foreach (var effect in resolvedLinks)
        {
            await _evidenceLinkRepository.AddAsync(effect.EvidenceLink, ct);
        }

        foreach (var decision in linkDecisions)
        {
            var proposal = draft.LinkProposals.Single(p => p.Id == decision.ProposalId);
            if (!decision.Accepted)
            {
                proposal.Reject();
                continue;
            }

            var effect = resolvedLinks.Single(e => e.ProposalId == decision.ProposalId);
            proposal.Accept(effect.EvidenceLink.Weight, effect.EvidenceLink.Rationale);
        }

        foreach (var effect in resolvedStudyItems)
        {
            await _studyItemRepository.AddAsync(effect.StudyItem, ct);
        }

        foreach (var decision in studyItemDecisions)
        {
            var proposal = draft.StudyItemProposals.Single(p => p.Id == decision.ProposalId);
            if (!decision.Accepted)
            {
                proposal.Reject();
                continue;
            }

            var effect = resolvedStudyItems.Single(e => e.ProposalId == decision.ProposalId);
            var studyItem = effect.StudyItem;
            proposal.Accept(studyItem.Title, studyItem.Category, studyItem.Details, studyItem.Tags, studyItem.Importance, studyItem.InitialMastery);
        }

        draft.MarkApplied(utcNow);
        await _draftRepository.SaveChangesAsync(ct);

        if (jobAnalysis is not null)
        {
            await _jobAnalysisRepository.SaveChangesAsync(ct);
        }

        if (cvPresentation is not null)
        {
            await _cvPresentationRepository.SaveChangesAsync(ct);
        }

        if (interviewNote is not null)
        {
            await _interviewNoteRepository.SaveChangesAsync(ct);
        }

        return ApplyAnalysisDraftOutcome.Applied;
    }

    private static void ValidateCoverage<TProposal, TDecision>(
        IReadOnlyList<TProposal> proposals, Func<TProposal, Guid> proposalId, IReadOnlyList<TDecision>? decisions, Func<TDecision, Guid> decisionId, string kindName)
    {
        if (decisions is null)
        {
            throw new ApplyAnalysisDraftValidationException($"{kindName} decisions must not be null.");
        }

        if (decisions.Any(decision => decision is null))
        {
            throw new ApplyAnalysisDraftValidationException($"{kindName} decisions must not contain a null entry.");
        }

        var proposalIds = proposals.Select(proposalId).ToHashSet();
        var decisionIds = decisions.Select(decisionId).ToList();

        if (decisionIds.Distinct().Count() != decisionIds.Count)
        {
            throw new ApplyAnalysisDraftValidationException($"{kindName} decisions must not reference the same proposal more than once.");
        }

        if (decisionIds.Count != proposalIds.Count || decisionIds.Any(id => !proposalIds.Contains(id)))
        {
            throw new ApplyAnalysisDraftValidationException($"{kindName} decisions must cover exactly this draft's own {kindName} proposals — no missing, no unknown.");
        }
    }

    private static IReadOnlyList<ResolvedSuggestionEffect> ResolveSuggestionDecisions(
        IReadOnlyList<SuggestionProposalDecision> decisions, AnalysisDraft draft, IReadOnlySet<StructuredSuggestionCommandType> allowedCommands, JobAnalysis? jobAnalysis)
    {
        // Every AddJobRequirement proposal's own decision, keyed by its already-assigned Guid —
        // resolves an AddJobGap's same-response dependency without needing the (not-yet-mutated)
        // live aggregate to already contain the new requirement.
        var requirementAssignedIdToAccepted = new Dictionary<Guid, bool>();
        foreach (var proposal in draft.SuggestionProposals)
        {
            if (proposal.ProposedPayload is StructuredSuggestion { CommandType: StructuredSuggestionCommandType.AddJobRequirement } structured)
            {
                var canonical = DeserializeJson<AddJobRequirementCanonicalPayload>(structured.PayloadJson, "proposed payload");
                var decision = decisions.Single(d => d.ProposalId == proposal.Id);
                requirementAssignedIdToAccepted[canonical.AssignedRequirementId] = decision.Accepted;
            }
        }

        var results = new List<ResolvedSuggestionEffect>();
        foreach (var decision in decisions)
        {
            var proposal = draft.SuggestionProposals.Single(p => p.Id == decision.ProposalId);

            if (!decision.Accepted)
            {
                if (decision.AcceptedPayloadJson is not null)
                {
                    throw new ApplyAnalysisDraftValidationException("A rejected SuggestionProposal must not carry an accepted payload.");
                }

                continue;
            }

            results.Add(proposal.ProposedPayload switch
            {
                AdvisorySuggestion => ResolveAdvisory(decision),
                StructuredSuggestion structured => ResolveStructured(decision, structured, allowedCommands, jobAnalysis, requirementAssignedIdToAccepted),
                _ => throw new ApplyAnalysisDraftValidationException("Unrecognized SuggestionPayload type."),
            });
        }

        return results;
    }

    private static ResolvedSuggestionEffect ResolveAdvisory(SuggestionProposalDecision decision)
    {
        if (decision.AcceptedPayloadJson is not null)
        {
            throw new ApplyAnalysisDraftValidationException("An accepted AdvisorySuggestion must not carry a separate accepted payload.");
        }

        return new ResolvedAdvisoryEffect { ProposalId = decision.ProposalId };
    }

    private static ResolvedSuggestionEffect ResolveStructured(
        SuggestionProposalDecision decision,
        StructuredSuggestion structured,
        IReadOnlySet<StructuredSuggestionCommandType> allowedCommands,
        JobAnalysis? jobAnalysis,
        IReadOnlyDictionary<Guid, bool> requirementAssignedIdToAccepted)
    {
        if (!allowedCommands.Contains(structured.CommandType))
        {
            throw new ApplyAnalysisDraftValidationException($"'{structured.CommandType}' is not a supported command for this draft's source.");
        }

        if (decision.AcceptedPayloadJson is null)
        {
            throw new ApplyAnalysisDraftValidationException("An accepted StructuredSuggestion requires a final payload.");
        }

        return structured.CommandType switch
        {
            StructuredSuggestionCommandType.AddJobRequirement => ResolveAddJobRequirement(decision.ProposalId, decision.AcceptedPayloadJson, structured.PayloadJson),
            StructuredSuggestionCommandType.AddJobGap => ResolveAddJobGap(decision.ProposalId, decision.AcceptedPayloadJson, structured.PayloadJson, jobAnalysis!, requirementAssignedIdToAccepted),
            StructuredSuggestionCommandType.UpdateCVPresentationSummary => ResolveUpdateCVPresentationSummary(decision.ProposalId, decision.AcceptedPayloadJson),
            StructuredSuggestionCommandType.AddInterviewGap => ResolveInterviewEntry(decision.ProposalId, decision.AcceptedPayloadJson, StructuredSuggestionCommandType.AddInterviewGap),
            StructuredSuggestionCommandType.AddInterviewLesson => ResolveInterviewEntry(decision.ProposalId, decision.AcceptedPayloadJson, StructuredSuggestionCommandType.AddInterviewLesson),
            _ => throw new ApplyAnalysisDraftValidationException($"'{structured.CommandType}' is not a supported command."),
        };
    }

    private static ResolvedSuggestionEffect ResolveAddJobRequirement(Guid proposalId, string acceptedPayloadJson, string proposedPayloadJson)
    {
        var decisionPayload = DeserializeJson<AddJobRequirementDecisionPayload>(acceptedPayloadJson, "final payload");
        var proposedCanonical = DeserializeJson<AddJobRequirementCanonicalPayload>(proposedPayloadJson, "proposed payload");

        var jobRequirement = Validate(() => new JobRequirement(
            proposedCanonical.AssignedRequirementId, decisionPayload.Text, decisionPayload.Kind, decisionPayload.Priority, decisionPayload.SourceExcerpt));

        return new ResolvedJobRequirementEffect { ProposalId = proposalId, JobRequirement = jobRequirement };
    }

    private static ResolvedSuggestionEffect ResolveAddJobGap(
        Guid proposalId, string acceptedPayloadJson, string proposedPayloadJson, JobAnalysis jobAnalysis, IReadOnlyDictionary<Guid, bool> requirementAssignedIdToAccepted)
    {
        var decisionPayload = DeserializeJson<AddJobGapDecisionPayload>(acceptedPayloadJson, "final payload");
        var proposedCanonical = DeserializeJson<AddJobGapCanonicalPayload>(proposedPayloadJson, "proposed payload");
        var requirementId = proposedCanonical.RequirementId;

        if (requirementAssignedIdToAccepted.TryGetValue(requirementId, out var siblingAccepted))
        {
            if (!siblingAccepted)
            {
                throw new ApplyAnalysisDraftValidationException("AddJobGap references an AddJobRequirement proposal that was not accepted.");
            }
        }
        else if (jobAnalysis.Requirements.All(r => r.Id != requirementId))
        {
            throw new ApplyAnalysisDraftValidationException("AddJobGap references a JobRequirement that no longer exists.");
        }

        var jobGap = Validate(() => new JobGap(Guid.NewGuid(), requirementId, decisionPayload.MatchLevel, decisionPayload.Severity, decisionPayload.Rationale));

        return new ResolvedJobGapEffect { ProposalId = proposalId, JobGap = jobGap };
    }

    private static ResolvedSuggestionEffect ResolveUpdateCVPresentationSummary(Guid proposalId, string acceptedPayloadJson)
    {
        var decisionPayload = DeserializeJson<UpdateCVPresentationSummaryPayload>(acceptedPayloadJson, "final payload");
        if (decisionPayload.SummaryMarkdown is not null && decisionPayload.SummaryMarkdown.Length > CommitAhead.Domain.ProfessionalProfiles.ValidationLimits.MarkdownMaxLength)
        {
            throw new ApplyAnalysisDraftValidationException("UpdateCVPresentationSummary.SummaryMarkdown is too long.");
        }

        return new ResolvedCVSummaryEffect { ProposalId = proposalId, SummaryMarkdownToSet = decisionPayload.SummaryMarkdown };
    }

    private static ResolvedSuggestionEffect ResolveInterviewEntry(Guid proposalId, string acceptedPayloadJson, StructuredSuggestionCommandType commandType)
    {
        var decisionPayload = DeserializeJson<InterviewNoteEntryPayload>(acceptedPayloadJson, "final payload");
        if (string.IsNullOrWhiteSpace(decisionPayload.Text) || decisionPayload.Text.Length > CommitAhead.Domain.InterviewNotes.ValidationLimits.ListEntryMaxLength)
        {
            throw new ApplyAnalysisDraftValidationException($"{commandType}.Text failed validation.");
        }

        return new ResolvedInterviewEntryEffect { ProposalId = proposalId, CommandType = commandType, TextToSet = decisionPayload.Text.Trim() };
    }

    private async Task<IReadOnlyList<ResolvedLinkEffect>> ResolveLinkDecisionsAsync(IReadOnlyList<LinkProposalDecision> decisions, AnalysisDraft draft, Guid ownerUserId, CancellationToken ct)
    {
        var results = new List<ResolvedLinkEffect>();
        var utcNow = DateTime.UtcNow;

        foreach (var decision in decisions)
        {
            if (!decision.Accepted)
            {
                if (decision.Weight is not null || decision.Rationale is not null)
                {
                    throw new ApplyAnalysisDraftValidationException("A rejected LinkProposal must not carry Weight/Rationale.");
                }

                continue;
            }

            if (decision.Weight is null || decision.Rationale is null)
            {
                throw new ApplyAnalysisDraftValidationException("An accepted LinkProposal requires Weight and Rationale.");
            }

            var proposal = draft.LinkProposals.Single(p => p.Id == decision.ProposalId);

            var targetStudyItem = await _studyItemRepository.GetByIdAsync(ownerUserId, proposal.TargetStudyItemId, ct);
            if (targetStudyItem is null)
            {
                throw new ApplyAnalysisDraftValidationException("LinkProposal's target StudyItem no longer exists.");
            }

            var alreadyExists = await _evidenceLinkRepository.ExistsAsync(ownerUserId, draft.SourceType, draft.SourceId, proposal.TargetStudyItemId, ct);
            if (alreadyExists)
            {
                throw new ApplyAnalysisDraftValidationException("An EvidenceLink for this source and target already exists.");
            }

            var evidenceLink = Validate(() => new EvidenceLink(
                Guid.NewGuid(), ownerUserId, draft.SourceType, draft.SourceId, proposal.TargetStudyItemId, decision.Weight.Value, decision.Rationale, utcNow));

            results.Add(new ResolvedLinkEffect { ProposalId = decision.ProposalId, EvidenceLink = evidenceLink });
        }

        return results;
    }

    private static IReadOnlyList<ResolvedStudyItemEffect> ResolveStudyItemDecisions(IReadOnlyList<StudyItemProposalDecision> decisions, Guid ownerUserId)
    {
        var results = new List<ResolvedStudyItemEffect>();
        var utcNow = DateTime.UtcNow;

        foreach (var decision in decisions)
        {
            if (!decision.Accepted)
            {
                if (decision.Title is not null || decision.Category is not null || decision.DetailsJson is not null
                    || decision.Tags is not null || decision.Importance is not null || decision.InitialMastery is not null)
                {
                    throw new ApplyAnalysisDraftValidationException("A rejected StudyItemProposal must not carry accepted fields.");
                }

                continue;
            }

            if (decision.Title is null || decision.Category is null || decision.DetailsJson is null
                || decision.Tags is null || decision.Importance is null || decision.InitialMastery is null)
            {
                throw new ApplyAnalysisDraftValidationException("An accepted StudyItemProposal requires every field, including InitialMastery.");
            }

            var details = ParseDetails(decision.Category.Value, decision.DetailsJson);
            var studyItem = Validate(() => new StudyItem(
                Guid.NewGuid(), ownerUserId, decision.Title, decision.Category.Value, decision.Importance.Value, decision.InitialMastery.Value, decision.Tags, details, utcNow));

            results.Add(new ResolvedStudyItemEffect { ProposalId = decision.ProposalId, StudyItem = studyItem });
        }

        return results;
    }

    private static StudyItemDetails ParseDetails(StudyItemCategory category, string detailsJson)
    {
        try
        {
            return StudyItemDetailsJsonParser.Parse(category, detailsJson);
        }
        catch (StudyItemDetailsPayloadException ex)
        {
            throw new ApplyAnalysisDraftValidationException(ex.Message);
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
            throw new ApplyAnalysisDraftValidationException($"Validation failed: {ex.Message}");
        }
    }

    private static T DeserializeJson<T>(string json, string what)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, StrictJsonOptions.Strict)
                ?? throw new ApplyAnalysisDraftValidationException($"The {what} must not be null.");
        }
        catch (JsonException)
        {
            throw new ApplyAnalysisDraftValidationException($"The {what} is not valid JSON for the declared command.");
        }
    }

    private abstract class ResolvedSuggestionEffect
    {
        public required Guid ProposalId { get; init; }

        public abstract SuggestionPayload? BuildAcceptedPayload();
    }

    private sealed class ResolvedAdvisoryEffect : ResolvedSuggestionEffect
    {
        public override SuggestionPayload? BuildAcceptedPayload() => null;
    }

    private sealed class ResolvedJobRequirementEffect : ResolvedSuggestionEffect
    {
        public required JobRequirement JobRequirement { get; init; }

        public override SuggestionPayload BuildAcceptedPayload() => new StructuredSuggestion(
            StructuredSuggestionCommandType.AddJobRequirement,
            JsonSerializer.Serialize(
                new AddJobRequirementCanonicalPayload(JobRequirement.Id, JobRequirement.Text, JobRequirement.Kind, JobRequirement.Priority, JobRequirement.SourceExcerpt),
                StrictJsonOptions.Strict));
    }

    private sealed class ResolvedJobGapEffect : ResolvedSuggestionEffect
    {
        public required JobGap JobGap { get; init; }

        public override SuggestionPayload BuildAcceptedPayload() => new StructuredSuggestion(
            StructuredSuggestionCommandType.AddJobGap,
            JsonSerializer.Serialize(new AddJobGapCanonicalPayload(JobGap.RequirementId, JobGap.MatchLevel, JobGap.Severity, JobGap.Rationale), StrictJsonOptions.Strict));
    }

    private sealed class ResolvedCVSummaryEffect : ResolvedSuggestionEffect
    {
        public required string? SummaryMarkdownToSet { get; init; }

        public string? AppliedSummaryMarkdown { get; set; }

        public override SuggestionPayload BuildAcceptedPayload() => new StructuredSuggestion(
            StructuredSuggestionCommandType.UpdateCVPresentationSummary,
            JsonSerializer.Serialize(new UpdateCVPresentationSummaryPayload(AppliedSummaryMarkdown), StrictJsonOptions.Strict));
    }

    private sealed class ResolvedInterviewEntryEffect : ResolvedSuggestionEffect
    {
        public required StructuredSuggestionCommandType CommandType { get; init; }

        public required string TextToSet { get; init; }

        public string? AppliedText { get; set; }

        public override SuggestionPayload BuildAcceptedPayload() => new StructuredSuggestion(
            CommandType, JsonSerializer.Serialize(new InterviewNoteEntryPayload(AppliedText!), StrictJsonOptions.Strict));
    }

    private sealed class ResolvedLinkEffect
    {
        public required Guid ProposalId { get; init; }

        public required EvidenceLink EvidenceLink { get; init; }
    }

    private sealed class ResolvedStudyItemEffect
    {
        public required Guid ProposalId { get; init; }

        public required StudyItem StudyItem { get; init; }
    }

    private sealed record AddJobRequirementDecisionPayload(string Text, JobRequirementKind Kind, JobRequirementPriority Priority, string SourceExcerpt);

    private sealed record AddJobGapDecisionPayload(JobGapMatchLevel MatchLevel, JobGapSeverity Severity, string Rationale);
}
