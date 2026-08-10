using System.Diagnostics;
using CommitAhead.Application.AI;
using CommitAhead.Application.Identity;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace CommitAhead.Infrastructure.Tests.AI;

/// <summary>
/// Real-Postgres proof that ADR-0014's reservation phase is durable — committed before the provider
/// is ever called, with no transaction held open around the external AI call, and completion/failure
/// each land in their own later, independently-committed transaction. A hand-simulated repository
/// sequence could not prove this; only a real database, observed from a second connection while the
/// first call is deliberately blocked mid-flight, can.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AnalysisCommandOrchestratorDurabilityTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public AnalysisCommandOrchestratorDurabilityTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = NewDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private CommitAheadDbContext NewDbContext() => new(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);

    private static AnalyzeJobAnalysisUseCase BuildUseCase(CommitAheadDbContext dbContext, Guid ownerUserId, BlockingAIProvider provider) => new(
        new JobAnalysisRepository(dbContext),
        new AnalysisDraftRepository(dbContext),
        new AIUsageRecordRepository(dbContext),
        new StudyItemRepository(dbContext),
        new ProfessionalProfileRepository(dbContext),
        provider,
        new RlsSessionContext(dbContext),
        new StubCurrentUser { UserId = ownerUserId },
        NullLogger<AnalyzeJobAnalysisUseCase>.Instance);

    private async Task<JobAnalysis> CreateJobAnalysisAsync(CommitAheadDbContext dbContext, Guid ownerUserId)
    {
        var jobAnalysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow);
        await new JobAnalysisRepository(dbContext).AddAsync(jobAnalysis, CancellationToken.None);
        return jobAnalysis;
    }

    /// <summary>Asserts no transaction anywhere is still holding this row's lock, by proving a raw FOR UPDATE from an independent connection returns instantly instead of waiting.</summary>
    private async Task AssertRowIsNotLockedAsync(Guid recordId)
    {
        await using var rawConnection = new NpgsqlConnection(_fixture.ConnectionString);
        await rawConnection.OpenAsync();

        await using (var setLockTimeout = new NpgsqlCommand("SET lock_timeout = '2000ms'", rawConnection))
        {
            await setLockTimeout.ExecuteNonQueryAsync();
        }

        await using var lockCheck = new NpgsqlCommand("SELECT 1 FROM ai_usage_records WHERE id = @id FOR UPDATE", rawConnection);
        lockCheck.Parameters.AddWithValue("id", recordId);

        var stopwatch = Stopwatch.StartNew();
        await lockCheck.ExecuteScalarAsync();
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 1500, $"Lock wait took {stopwatch.ElapsedMilliseconds}ms — a transaction is still holding this row.");
    }

    [Fact]
    public async Task ExecuteAsync_WhileTheProviderCallIsBlocked_ReservationIsAlreadyCommittedAndNoTransactionIsHeldOpen()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysis = await CreateJobAnalysisAsync(_dbContext, ownerUserId);

        await using var callerDbContext = NewDbContext();
        var provider = new BlockingAIProvider();
        var useCase = BuildUseCase(callerDbContext, ownerUserId, provider);

        var analyzeTask = useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);
        await provider.EnteredProviderCall;

        // A second, independent connection can already see the reservation — Phase A committed.
        await using var observerDbContext = NewDbContext();
        var reserved = await new AIUsageRecordRepository(observerDbContext).GetActiveReservationByOwnerAsync(ownerUserId, CancellationToken.None);
        Assert.NotNull(reserved);
        Assert.Equal(AIUsageRecordStatus.Reserved, reserved!.Status);

        await AssertRowIsNotLockedAsync(reserved.Id);

        provider.Release();
        var result = await analyzeTask;

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        Assert.NotNull(result.AnalysisDraftId);

        await using var reloadDbContext = NewDbContext();
        var completed = await new AIUsageRecordRepository(reloadDbContext).GetByIdempotencyKeyAsync(ownerUserId, "key-1", CancellationToken.None);
        Assert.NotNull(completed);
        Assert.Equal(AIUsageRecordStatus.Completed, completed!.Status);
        Assert.Equal(result.AnalysisDraftId, completed.AnalysisDraftId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheProviderThrowsAfterACommittedReservation_TheDurableReservationBecomesFailed()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysis = await CreateJobAnalysisAsync(_dbContext, ownerUserId);

        await using var callerDbContext = NewDbContext();
        var provider = new BlockingAIProvider { ThrowOnRelease = true };
        var useCase = BuildUseCase(callerDbContext, ownerUserId, provider);

        var analyzeTask = useCase.ExecuteAsync(jobAnalysis.Id, "key-2", CancellationToken.None);
        await provider.EnteredProviderCall;

        await using var observerDbContext = NewDbContext();
        var reserved = await new AIUsageRecordRepository(observerDbContext).GetActiveReservationByOwnerAsync(ownerUserId, CancellationToken.None);
        Assert.NotNull(reserved);

        provider.Release();
        await Assert.ThrowsAsync<InvalidOperationException>(() => analyzeTask);

        await using var reloadDbContext = NewDbContext();
        var failed = await new AIUsageRecordRepository(reloadDbContext).GetByIdempotencyKeyAsync(ownerUserId, "key-2", CancellationToken.None);
        Assert.NotNull(failed);
        Assert.Equal(AIUsageRecordStatus.Failed, failed!.Status);
        Assert.Null(failed.AnalysisDraftId);
    }

    [Fact]
    public async Task ExecuteAsync_WhileAnotherAnalysisIsBlockedForTheSameOwner_TheSecondCallNeverInvokesTheProvider()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysis = await CreateJobAnalysisAsync(_dbContext, ownerUserId);

        await using var firstDbContext = NewDbContext();
        var firstProvider = new BlockingAIProvider();
        var firstUseCase = BuildUseCase(firstDbContext, ownerUserId, firstProvider);
        var firstTask = firstUseCase.ExecuteAsync(jobAnalysis.Id, "key-first", CancellationToken.None);
        await firstProvider.EnteredProviderCall;

        await using var secondDbContext = NewDbContext();
        var secondProvider = new BlockingAIProvider();
        var secondUseCase = BuildUseCase(secondDbContext, ownerUserId, secondProvider);
        var secondResult = await secondUseCase.ExecuteAsync(jobAnalysis.Id, "key-second", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.AnotherAnalysisInProgress, secondResult.Outcome);
        Assert.Equal(0, secondProvider.CallCount);

        firstProvider.Release();
        var firstResult = await firstTask;
        Assert.Equal(AnalyzeCommandOutcome.Created, firstResult.Outcome);
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public required Guid UserId { get; init; }

        public string Email => "owner@example.com";
    }

    /// <summary>Gates AnalyzeJobAnalysisAsync on an externally-controlled release so the test can observe database state while the "provider call" is still in flight.</summary>
    private sealed class BlockingAIProvider : IAIProvider
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ThrowOnRelease { get; set; }

        public int CallCount { get; private set; }

        public Task EnteredProviderCall => _entered.Task;

        public AiProviderDescriptor Describe(AiCommandType commandType) => new(
            Provider: "blocking-fake",
            Model: "fake-model",
            PricingVersion: "fake-v1",
            Currency: "USD",
            MaxInputTokens: 8_000,
            MaxOutputTokens: 2_000,
            Timeout: TimeSpan.FromSeconds(30),
            EstimatedMaxCost: 0m);

        public async Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken)
        {
            CallCount++;
            _entered.TrySetResult();
            await _release.Task;

            if (ThrowOnRelease)
            {
                throw new InvalidOperationException("Simulated provider failure.");
            }

            return new AiAnalysisResult([], [], [], InputTokens: 10, OutputTokens: 5, ActualCost: 0m);
        }

        public Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test fake only exercises AnalyzeJobAnalysisAsync.");

        public Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test fake only exercises AnalyzeJobAnalysisAsync.");

        public void Release() => _release.TrySetResult();
    }
}
