using CommitAhead.Application.AI;

namespace CommitAhead.Application.Tests.AI;

public class FakeAIProviderTests
{
    private static readonly AiCallLimits Limits = new(MaxInputTokens: 2000, MaxOutputTokens: 500, Timeout: TimeSpan.FromSeconds(10));

    private static JobAnalysisAiInput CreateInput() => new("Job posting text.", ["C#", "PostgreSQL"], []);

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithSuccessScenario_ReturnsOneProposalOfEachKind()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.Success };

        var result = await provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None);

        Assert.Single(result.SuggestionProposals);
        Assert.Single(result.LinkProposals);
        Assert.Single(result.StudyItemProposals);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithEmptyOutputScenario_ReturnsNoProposals()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.EmptyOutput };

        var result = await provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None);

        Assert.Empty(result.SuggestionProposals);
        Assert.Empty(result.LinkProposals);
        Assert.Empty(result.StudyItemProposals);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithMalformedProposalsScenario_ReturnsALinkProposalWithAnOutOfRangeWeight()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.MalformedProposals };

        var result = await provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None);

        var link = Assert.Single(result.LinkProposals);
        Assert.True(link.Weight is < 0 or > 5);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithDuplicatesScenario_ReturnsTwoLinkProposalsForTheSameTarget()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.Duplicates };

        var result = await provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None);

        Assert.Equal(2, result.LinkProposals.Count);
        Assert.Equal(result.LinkProposals[0].TargetStudyItemId, result.LinkProposals[1].TargetStudyItemId);
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithTimeoutScenario_ThrowsTimeoutException()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.Timeout };

        await Assert.ThrowsAsync<TimeoutException>(() => provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeJobAnalysisAsync_WithProviderFailureScenario_ThrowsHttpRequestException()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.ProviderFailure };

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.AnalyzeJobAnalysisAsync(CreateInput(), Limits, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeCVPresentationAsync_AndAnalyzeInterviewNoteAsync_ShareTheSameScenarios()
    {
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.EmptyOutput };

        var cvResult = await provider.AnalyzeCVPresentationAsync(new CVPresentationAiInput(null, [], [], [], []), Limits, CancellationToken.None);
        var interviewResult = await provider.AnalyzeInterviewNoteAsync(new InterviewNoteAiInput("Acme", "Engineer", "Technical", [], [], [], []), Limits, CancellationToken.None);

        Assert.Empty(cvResult.LinkProposals);
        Assert.Empty(interviewResult.LinkProposals);
    }
}
