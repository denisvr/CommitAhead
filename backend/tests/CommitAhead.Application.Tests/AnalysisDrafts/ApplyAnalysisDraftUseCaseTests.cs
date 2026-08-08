using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.InterviewNotes;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.AnalysisDrafts;

public class ApplyAnalysisDraftUseCaseTests
{
    private static ApplyAnalysisDraftUseCase CreateUseCase(
        FakeAnalysisDraftRepository draftRepository,
        Guid ownerUserId,
        FakeJobAnalysisRepository? jobAnalysisRepository = null,
        FakeCVPresentationRepository? cvPresentationRepository = null,
        FakeInterviewNoteRepository? interviewNoteRepository = null,
        FakeStudyItemRepository? studyItemRepository = null,
        FakeEvidenceLinkRepository? evidenceLinkRepository = null)
        => new(
            draftRepository,
            jobAnalysisRepository ?? new FakeJobAnalysisRepository(),
            cvPresentationRepository ?? new FakeCVPresentationRepository(),
            interviewNoteRepository ?? new FakeInterviewNoteRepository(),
            studyItemRepository ?? new FakeStudyItemRepository(),
            evidenceLinkRepository ?? new FakeEvidenceLinkRepository(),
            new FakeUnitOfWork(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    private static JobAnalysis CreateJobAnalysis(Guid ownerUserId) =>
        new(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("We need 5+ years of C# and PostgreSQL."), null, DateTime.UtcNow);

    private static StudyItem CreateStudyItem(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "PostgreSQL Indexing", StudyItemCategory.Theory, 3, 2, ["databases"],
        new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);

    private static string AddJobRequirementCanonicalJson(Guid assignedRequirementId, string text = "5+ years of C#") =>
        $$"""{"AssignedRequirementId":"{{assignedRequirementId}}","Text":"{{text}}","Kind":"Technical","Priority":"Required","SourceExcerpt":"5+ years of C# required."}""";

    private static string AddJobRequirementDecisionJson(string text = "5+ years of C# (finalised)") =>
        $$"""{"Text":"{{text}}","Kind":"Technical","Priority":"Required","SourceExcerpt":"5+ years of C# required."}""";

    private static string AddJobGapCanonicalJson(Guid requirementId) =>
        $$"""{"RequirementId":"{{requirementId}}","MatchLevel":"Missing","Severity":"High","Rationale":"No PostgreSQL experience found."}""";

    private static string AddJobGapDecisionJson(string rationale = "No PostgreSQL experience found (finalised).") =>
        $$"""{"MatchLevel":"Missing","Severity":"High","Rationale":"{{rationale}}"}""";

    private static string UpdateSummaryJson(string? summaryMarkdown) =>
        summaryMarkdown is null ? """{"SummaryMarkdown":null}""" : $$"""{"SummaryMarkdown":"{{summaryMarkdown}}"}""";

    private static string EntryJson(string text) => $$"""{"Text":"{{text}}"}""";

    private static string TheoryDetailsJson() => """{"SummaryMarkdown":"Summary","KeyPoints":["Point"],"InterviewQuestions":["Question?"],"References":["https://example.com"]}""";

    [Fact]
    public async Task ExecuteAsync_JobAnalysisHappyPath_AppliesRequirementGapLinkAndStudyItem()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var studyItemRepository = new FakeStudyItemRepository();
        var targetStudyItem = CreateStudyItem(ownerUserId);
        await studyItemRepository.AddAsync(targetStudyItem, CancellationToken.None);

        var assignedRequirementId = Guid.NewGuid();
        var requirementProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementCanonicalJson(assignedRequirementId)));
        var gapProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobGap, AddJobGapCanonicalJson(assignedRequirementId)));
        var linkProposal = new LinkProposal(Guid.NewGuid(), targetStudyItem.Id, 3, "Directly demonstrates this skill.");
        var studyItemProposal = new StudyItemProposal(Guid.NewGuid(), "Consistent Hashing", StudyItemCategory.Theory, new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), ["distributed-systems"], 4);

        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [requirementProposal, gapProposal], [linkProposal], [studyItemProposal], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var evidenceLinkRepository = new FakeEvidenceLinkRepository();
        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository, studyItemRepository: studyItemRepository, evidenceLinkRepository: evidenceLinkRepository);

        var result = await useCase.ExecuteAsync(
            draft.Id,
            [
                new SuggestionProposalDecision(requirementProposal.Id, true, AddJobRequirementDecisionJson()),
                new SuggestionProposalDecision(gapProposal.Id, true, AddJobGapDecisionJson()),
            ],
            [new LinkProposalDecision(linkProposal.Id, true, 4, "Confirmed after review.")],
            [new StudyItemProposalDecision(studyItemProposal.Id, true, "Consistent Hashing", StudyItemCategory.Theory, TheoryDetailsJson(), ["distributed-systems"], 4, 2)],
            CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Equal(AnalysisDraftStatus.Applied, draft.Status);

        var requirement = Assert.Single(jobAnalysis.Requirements);
        Assert.Equal(assignedRequirementId, requirement.Id);
        Assert.Equal("5+ years of C# (finalised)", requirement.Text);
        var gap = Assert.Single(jobAnalysis.Gaps);
        Assert.Equal(assignedRequirementId, gap.RequirementId);

        var requirementPayload = (StructuredSuggestion)requirementProposal.AcceptedPayload!;
        Assert.Contains(assignedRequirementId.ToString(), requirementPayload.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("finalised", requirementPayload.PayloadJson, StringComparison.Ordinal);

        Assert.Equal(4m, linkProposal.AcceptedWeight);
        Assert.Equal("Confirmed after review.", linkProposal.AcceptedRationale);
        var evidenceLink = Assert.Single(evidenceLinkRepository.Links);
        Assert.Equal(targetStudyItem.Id, evidenceLink.TargetStudyItemId);
        Assert.Equal(4m, evidenceLink.Weight);

        Assert.Equal("Consistent Hashing", studyItemProposal.AcceptedTitle);
        Assert.Equal(2, studyItemProposal.AcceptedInitialMastery);
        var createdStudyItem = Assert.Single(studyItemRepository.Items, item => item.Id != targetStudyItem.Id);
        Assert.Equal("Consistent Hashing", createdStudyItem.Title);
        Assert.Equal(2, createdStudyItem.InitialMastery);
    }

    [Fact]
    public async Task ExecuteAsync_CVPresentationHappyPath_UpdatesSummaryOverride()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, new ContactInfo("Ada Lovelace", "ada@example.com", null, null, null), "Canonical summary.", DateTime.UtcNow);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);

        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.UpdateCVPresentationSummary, UpdateSummaryJson("AI-proposed summary.")));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, presentation.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, cvPresentationRepository: cvRepository);

        var result = await useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(proposal.Id, true, UpdateSummaryJson("User-finalised summary."))], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Equal("User-finalised summary.", presentation.SummaryOverrideMarkdown);
        var payload = (StructuredSuggestion)proposal.AcceptedPayload!;
        Assert.Contains("User-finalised summary.", payload.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_CVPresentationSummaryCleared_AllowsNullFinalPayload()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, new ContactInfo("Ada Lovelace", "ada@example.com", null, null, null), "Canonical summary.", DateTime.UtcNow);
        var presentation = new CVPresentation(Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", "Existing override.", false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);

        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.UpdateCVPresentationSummary, UpdateSummaryJson("AI-proposed summary.")));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.CVPresentation, presentation.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, cvPresentationRepository: cvRepository);

        var result = await useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(proposal.Id, true, UpdateSummaryJson(null))], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Null(presentation.SummaryOverrideMarkdown);
    }

    [Fact]
    public async Task ExecuteAsync_InterviewNoteHappyPath_MergesGapAndLessonIntoOneUpdate()
    {
        var ownerUserId = Guid.NewGuid();
        var noteRepository = new FakeInterviewNoteRepository();
        var note = new InterviewNote(
            Guid.NewGuid(), ownerUserId, "Acme", "Backend Engineer", InterviewRound.Technical, 1, null,
            new DateOnly(2026, 1, 15), ["Tell me about a distributed system you built."], ["Existing gap"], ["Existing lesson"], null, DateTime.UtcNow);
        await noteRepository.AddAsync(note, CancellationToken.None);

        var gapProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddInterviewGap, EntryJson("System design depth is weak.")));
        var lessonProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddInterviewLesson, EntryJson("Review consistent hashing.")));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.InterviewNote, note.Id, [gapProposal, lessonProposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, interviewNoteRepository: noteRepository);

        var result = await useCase.ExecuteAsync(
            draft.Id,
            [
                new SuggestionProposalDecision(gapProposal.Id, true, EntryJson("System design depth is weak (finalised).")),
                new SuggestionProposalDecision(lessonProposal.Id, true, EntryJson("Review consistent hashing (finalised).")),
            ],
            [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Equal(["Existing gap", "System design depth is weak (finalised)."], note.Gaps);
        Assert.Equal(["Existing lesson", "Review consistent hashing (finalised)."], note.Lessons);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnAcceptedAdvisorySuggestion_AcceptsWithNoSourceMutation()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var advisoryProposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider clarifying the seniority level with the recruiter."));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [advisoryProposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        var result = await useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(advisoryProposal.Id, true, null)], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Equal(ProposalStatus.Accepted, advisoryProposal.Status);
        Assert.Null(advisoryProposal.AcceptedPayload);
        Assert.Empty(jobAnalysis.Requirements);
    }

    [Fact]
    public async Task ExecuteAsync_WithARejectedProposal_ProducesNoEffect()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var assignedRequirementId = Guid.NewGuid();
        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementCanonicalJson(assignedRequirementId)));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        var result = await useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(proposal.Id, false, null)], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.Applied, result);
        Assert.Equal(ProposalStatus.Rejected, proposal.Status);
        Assert.Empty(jobAnalysis.Requirements);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnknownDraftId_ReturnsDraftNotFound()
    {
        var useCase = CreateUseCase(new FakeAnalysisDraftRepository(), Guid.NewGuid());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), [], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.DraftNotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnAlreadyAppliedDraft_ReturnsDraftNotPending()
    {
        var ownerUserId = Guid.NewGuid();
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        draft.MarkApplied(DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, [], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.DraftNotPending, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithAMissingSource_ReturnsSourceNotFound()
    {
        var ownerUserId = Guid.NewGuid();
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, [], [], [], CancellationToken.None);

        Assert.Equal(ApplyAnalysisDraftOutcome.SourceNotFound, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithAMissingSuggestionDecision_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Advice."));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(() => useCase.ExecuteAsync(draft.Id, [], [], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithADuplicateSuggestionDecision_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Advice."));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        var decisions = new[]
        {
            new SuggestionProposalDecision(proposal.Id, true, null),
            new SuggestionProposalDecision(proposal.Id, true, null),
        };

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(() => useCase.ExecuteAsync(draft.Id, decisions, [], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithADecisionForAnUnknownProposalId_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(
            () => useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(Guid.NewGuid(), false, null)], [], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithADecisionInTheWrongTypedList_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var suggestionProposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Advice."));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [suggestionProposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        // suggestionProposal.Id is a real Guid in this draft, but it belongs to SuggestionProposals,
        // not LinkProposals — must still be rejected as unknown for the LinkProposal list.
        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(() => useCase.ExecuteAsync(
            draft.Id,
            [new SuggestionProposalDecision(suggestionProposal.Id, true, null)],
            [new LinkProposalDecision(suggestionProposal.Id, false, null, null)],
            [],
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnsupportedCommandForTheSource_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.UpdateCVPresentationSummary, UpdateSummaryJson("Mismatched.")));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [proposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(
            () => useCase.ExecuteAsync(draft.Id, [new SuggestionProposalDecision(proposal.Id, true, UpdateSummaryJson("Mismatched."))], [], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAGapReferencingARejectedRequirement_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var assignedRequirementId = Guid.NewGuid();
        var requirementProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementCanonicalJson(assignedRequirementId)));
        var gapProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobGap, AddJobGapCanonicalJson(assignedRequirementId)));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [requirementProposal, gapProposal], [], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(() => useCase.ExecuteAsync(
            draft.Id,
            [
                new SuggestionProposalDecision(requirementProposal.Id, false, null),
                new SuggestionProposalDecision(gapProposal.Id, true, AddJobGapDecisionJson()),
            ],
            [], [], CancellationToken.None));

        Assert.Empty(jobAnalysis.Requirements);
        Assert.Empty(jobAnalysis.Gaps);
    }

    [Fact]
    public async Task ExecuteAsync_WithALinkProposalTargetingAMissingStudyItem_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var linkProposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Some rationale.");
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [linkProposal], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(
            () => useCase.ExecuteAsync(draft.Id, [], [new LinkProposalDecision(linkProposal.Id, true, 3, "Confirmed.")], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnAlreadyExistingEvidenceLink_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var studyItemRepository = new FakeStudyItemRepository();
        var targetStudyItem = CreateStudyItem(ownerUserId);
        await studyItemRepository.AddAsync(targetStudyItem, CancellationToken.None);

        var evidenceLinkRepository = new FakeEvidenceLinkRepository();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, targetStudyItem.Id, 2, "Already linked.", DateTime.UtcNow),
            CancellationToken.None);

        var linkProposal = new LinkProposal(Guid.NewGuid(), targetStudyItem.Id, 3, "Some rationale.");
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [linkProposal], [], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository, studyItemRepository: studyItemRepository, evidenceLinkRepository: evidenceLinkRepository);

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(
            () => useCase.ExecuteAsync(draft.Id, [], [new LinkProposalDecision(linkProposal.Id, true, 3, "Confirmed.")], [], CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithATooLongFinalStudyItemDetailsPayload_ThrowsApplyAnalysisDraftValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var studyItemProposal = new StudyItemProposal(Guid.NewGuid(), "Consistent Hashing", StudyItemCategory.Theory, new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), ["tag"], 4);
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [], [studyItemProposal], DateTime.UtcNow);
        var draftRepository = new FakeAnalysisDraftRepository();
        await draftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(draftRepository, ownerUserId, jobAnalysisRepository);

        const string malformedDetailsJson = """{"SummaryMarkdown":"Summary","KeyPoints":["Point"],"InterviewQuestions":["Question?"],"References":["https://example.com"],"Unexpected":"value"}""";

        await Assert.ThrowsAsync<ApplyAnalysisDraftValidationException>(() => useCase.ExecuteAsync(
            draft.Id, [], [],
            [new StudyItemProposalDecision(studyItemProposal.Id, true, "Consistent Hashing", StudyItemCategory.Theory, malformedDetailsJson, ["tag"], 4, 2)],
            CancellationToken.None));
    }
}
