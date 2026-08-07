using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.InterviewNotes;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.InterviewNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

public class AnalyzeInterviewNoteUseCaseTests
{
    private static AnalyzeInterviewNoteUseCase CreateUseCase(
        FakeInterviewNoteRepository noteRepository,
        FakeAnalysisDraftRepository draftRepository,
        FakeAIUsageRecordRepository usageRepository,
        IAIProvider aiProvider,
        Guid ownerUserId,
        FakeStudyItemRepository? studyItemRepository = null)
        => new(
            noteRepository,
            draftRepository,
            usageRepository,
            studyItemRepository ?? new FakeStudyItemRepository(),
            aiProvider,
            new FakeUnitOfWork(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
            NullLogger<AnalyzeInterviewNoteUseCase>.Instance);

    private static InterviewNote CreateNote(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Acme", "Backend Engineer", InterviewRound.Technical, 1, null,
        new DateOnly(2026, 1, 15), ["Tell me about a distributed system you built."], ["No PostgreSQL depth"], ["Review consistent hashing"], null, DateTime.UtcNow);

    private static string EntryPayload(string text) => $$"""{"Text":"{{text}}"}""";

    [Theory]
    [InlineData(StructuredSuggestionCommandType.AddInterviewGap)]
    [InlineData(StructuredSuggestionCommandType.AddInterviewLesson)]
    public async Task ExecuteAsync_WithAValidEntryProposal_CreatesTheDraft(StructuredSuggestionCommandType commandType)
    {
        var ownerUserId = Guid.NewGuid();
        var noteRepository = new FakeInterviewNoteRepository();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(commandType, EntryPayload("System design depth is weak."), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 50,
                OutputTokens: 20,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(noteRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(note.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        var draft = Assert.Single(draftRepository.Drafts);
        var payload = (StructuredSuggestion)Assert.Single(draft.SuggestionProposals).ProposedPayload;
        Assert.Equal(commandType, payload.CommandType);
        Assert.Contains("System design depth is weak.", payload.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnsupportedCommandType_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var noteRepository = new FakeInterviewNoteRepository();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.UpdateCVPresentationSummary, "{}", null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(noteRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(note.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithATooLongEntry_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var noteRepository = new FakeInterviewNoteRepository();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var tooLong = new string('a', 501);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddInterviewGap, EntryPayload(tooLong), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(noteRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(note.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnknownInterviewNoteId_ReturnsSourceNotFound()
    {
        var ownerUserId = Guid.NewGuid();
        var useCase = CreateUseCase(new FakeInterviewNoteRepository(), new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), new ScriptedAIProvider(), ownerUserId);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.SourceNotFound, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheNotesOwnFields()
    {
        var ownerUserId = Guid.NewGuid();
        var noteRepository = new FakeInterviewNoteRepository();
        var note = CreateNote(ownerUserId);
        await noteRepository.AddAsync(note, CancellationToken.None);
        var provider = new ScriptedAIProvider { Result = new AiAnalysisResult([], [], [], 10, 10, 0m) };
        var useCase = CreateUseCase(noteRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await useCase.ExecuteAsync(note.Id, "key-1", CancellationToken.None);

        var input = provider.LastInterviewNoteInput!;
        Assert.Equal("Acme", input.Company);
        Assert.Equal("Backend Engineer", input.Role);
        Assert.Equal("Technical", input.InterviewRound);
        Assert.Equal(note.Questions, input.Questions);
        Assert.Equal(note.Gaps, input.Gaps);
        Assert.Equal(note.Lessons, input.Lessons);
    }
}
