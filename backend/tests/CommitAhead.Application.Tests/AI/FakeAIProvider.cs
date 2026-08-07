using CommitAhead.Application.AI;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// Handwritten deterministic fake (ADR-0009) — the only IAIProvider implementation used in
/// automated tests and CI, per CLAUDE.md's "Zero real AI calls in CI" hard constraint. Every
/// analyze method shares the same six scenarios (docs/roadmap.md "Implement FakeAIProvider with
/// six deterministic scenarios per command") since the scenario a test wants to simulate doesn't
/// depend on which command is being analyzed.
/// </summary>
public sealed class FakeAIProvider : IAIProvider
{
    public FakeAIScenario Scenario { get; set; } = FakeAIScenario.Success;

    public Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync(cancellationToken);

    public Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync(cancellationToken);

    public Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync(cancellationToken);

    private Task<AiAnalysisResult> ProduceResultAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Scenario switch
        {
            FakeAIScenario.Success => Task.FromResult(SuccessResult()),
            FakeAIScenario.EmptyOutput => Task.FromResult(EmptyResult()),
            FakeAIScenario.MalformedProposals => Task.FromResult(MalformedResult()),
            FakeAIScenario.Duplicates => Task.FromResult(DuplicatesResult()),
            FakeAIScenario.Timeout => throw new TimeoutException("Simulated provider timeout."),
            FakeAIScenario.ProviderFailure => throw new HttpRequestException("Simulated provider failure."),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario), Scenario, "Unrecognized scenario."),
        };
    }

    private static AiAnalysisResult SuccessResult() => new(
        SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, "{\"text\":\"5+ years of C#\"}", null)],
        LinkProposals: [new AiLinkProposal(Guid.NewGuid(), 3, "Directly demonstrates this skill.")],
        StudyItemProposals: [new AiStudyItemProposal("Consistent Hashing", StudyItemCategory.Theory, "{\"summaryMarkdown\":\"...\"}", ["distributed-systems"], 4)],
        InputTokens: 500,
        OutputTokens: 150);

    private static AiAnalysisResult EmptyResult() => new([], [], [], InputTokens: 400, OutputTokens: 10);

    /// <summary>Weight 10 is out of LinkProposal's [0,5] range — an analyzing use case must reject this proposal, not construct a Domain LinkProposal from it.</summary>
    private static AiAnalysisResult MalformedResult() => new(
        SuggestionProposals: [],
        LinkProposals: [new AiLinkProposal(Guid.NewGuid(), 10, "Weight is out of range.")],
        StudyItemProposals: [],
        InputTokens: 500,
        OutputTokens: 50);

    private static AiAnalysisResult DuplicatesResult()
    {
        var duplicateTargetId = Guid.NewGuid();
        return new AiAnalysisResult(
            SuggestionProposals: [],
            LinkProposals:
            [
                new AiLinkProposal(duplicateTargetId, 3, "First proposal for this StudyItem."),
                new AiLinkProposal(duplicateTargetId, 4, "Second, duplicate proposal for the same StudyItem."),
            ],
            StudyItemProposals: [],
            InputTokens: 500,
            OutputTokens: 80);
    }
}
