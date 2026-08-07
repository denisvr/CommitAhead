using CommitAhead.Domain;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Domain.Tests.AIUsage;

public class AIUsageRecordTests
{
    private static readonly DateTime StartedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AIUsageRecord CreateRecord() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "idempotency-key-1",
        AiCommandType.AnalyzeJobAnalysis,
        EvidenceSourceType.JobAnalysis,
        Guid.NewGuid(),
        "anthropic",
        "claude-fake",
        "2026-01-01",
        "usd",
        1000,
        500,
        0.05m,
        StartedAt);

    [Fact]
    public void Constructor_WithValidArguments_StartsReserved_AndUppercasesCurrency()
    {
        var record = CreateRecord();

        Assert.Equal(AIUsageRecordStatus.Reserved, record.Status);
        Assert.Equal("USD", record.Currency);
        Assert.Equal(StartedAt, record.StartedAtUtc);
        Assert.Null(record.CompletedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptySourceId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new AIUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), "key", AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.Empty,
            "anthropic", "claude-fake", "2026-01-01", "usd", 100, 100, 0, StartedAt));
    }

    [Fact]
    public void Constructor_WithNegativeReservedCost_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new AIUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), "key", AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            "anthropic", "claude-fake", "2026-01-01", "usd", 100, 100, -0.01m, StartedAt));
    }

    [Fact]
    public void Constructor_WithACurrencyThatIsNotThreeCharacters_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new AIUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), "key", AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            "anthropic", "claude-fake", "2026-01-01", "dollars", 100, 100, 0, StartedAt));
    }

    [Fact]
    public void Complete_SetsActualUsageAndAnalysisDraftId_AndTransitions()
    {
        var record = CreateRecord();
        var draftId = Guid.NewGuid();

        record.Complete(900, 450, 0.045m, draftId, "success", StartedAt.AddSeconds(3));

        Assert.Equal(AIUsageRecordStatus.Completed, record.Status);
        Assert.Equal(900, record.ActualInputTokens);
        Assert.Equal(450, record.ActualOutputTokens);
        Assert.Equal(0.045m, record.ActualCost);
        Assert.Equal(draftId, record.AnalysisDraftId);
        Assert.Equal("success", record.OutcomeCode);
        Assert.Equal(StartedAt.AddSeconds(3), record.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WithAnEmptyAnalysisDraftId_Throws()
    {
        var record = CreateRecord();

        Assert.Throws<DomainValidationException>(() => record.Complete(900, 450, 0.045m, Guid.Empty, "success", StartedAt.AddSeconds(3)));
    }

    [Fact]
    public void Fail_TransitionsToFailed_WithNoActualUsage()
    {
        var record = CreateRecord();

        record.Fail("provider-timeout", StartedAt.AddSeconds(10));

        Assert.Equal(AIUsageRecordStatus.Failed, record.Status);
        Assert.Equal("provider-timeout", record.OutcomeCode);
        Assert.Null(record.ActualCost);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_Throws()
    {
        var record = CreateRecord();
        record.Complete(900, 450, 0.045m, Guid.NewGuid(), "success", StartedAt.AddSeconds(3));

        Assert.Throws<DomainValidationException>(() => record.Complete(900, 450, 0.045m, Guid.NewGuid(), "success", StartedAt.AddSeconds(4)));
    }

    [Fact]
    public void Fail_WhenAlreadyFailed_Throws()
    {
        var record = CreateRecord();
        record.Fail("provider-timeout", StartedAt.AddSeconds(10));

        Assert.Throws<DomainValidationException>(() => record.Fail("provider-timeout", StartedAt.AddSeconds(11)));
    }
}
