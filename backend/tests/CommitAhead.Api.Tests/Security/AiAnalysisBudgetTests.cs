using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Application.AI;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Api.Tests.Security;

/// <summary>ADR-0019's per-owner daily/monthly AI budget, enforced end-to-end through the real HTTP surface.</summary>
[Collection(StudyItemsApiCollection.Name)]
public class AiAnalysisBudgetTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public AiAnalysisBudgetTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedCompletedUsageAsync(Guid ownerUserId, decimal actualCost)
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_factory.ConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        var repository = new AIUsageRecordRepository(dbContext);

        var record = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, Guid.NewGuid().ToString(), AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            "fake", "fake-test-model", "fake-v1", "USD", 1000, 500, actualCost, DateTime.UtcNow);
        record.Complete(100, 50, actualCost, Guid.NewGuid(), "success", DateTime.UtcNow);
        await repository.AddAsync(record, CancellationToken.None);
    }

    [Fact]
    public async Task Analyze_WhenTodaysSpendIsAlreadyAtTheDailyLimit_Returns429WithRetryAfterAndAStableOutcomeCode()
    {
        var ownerUserId = Guid.NewGuid();
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(ownerUserId);
        // FakeAIProvider.Describe() reports EstimatedMaxCost: 0m, so the "spent + estimate" check
        // needs the already-spent amount alone to exceed the daily limit.
        await SeedCompletedUsageAsync(ownerUserId, 0.26m);

        var postResponse = await client.SendMutatingAsync(
            HttpMethod.Post, "/api/job-analyses", accessCookie, new CreateJobAnalysisRequest("Senior Backend Engineer", "Job posting text.", null));
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var response = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(nameof(AnalyzeCommandOutcome.DailyBudgetExceeded), problem!.Extensions["outcomeCode"]!.ToString());
    }
}
