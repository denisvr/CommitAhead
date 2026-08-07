using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class DeleteJobAnalysisUseCaseTests
{
    private static JobAnalysis CreatePastedTextAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);

    private static JobAnalysis CreateUploadedFileAnalysis(Guid ownerUserId, string storageObjectKey) => new(
        Guid.NewGuid(), ownerUserId, "Title", new UploadedFile(storageObjectKey, "posting.pdf", "application/pdf", "Extracted text."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingAnalysis_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreatePastedTextAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new DeleteJobAnalysisUseCase(
            repository, storage, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingAnalysis_ReturnsNotFound()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var useCase = new DeleteJobAnalysisUseCase(
            repository, storage, new StubCurrentUser { UserId = Guid.NewGuid(), Email = "owner@example.com" }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.NotFound, result);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task ExecuteAsync_WithAPastedTextSource_NeverCallsStorage()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreatePastedTextAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new DeleteJobAnalysisUseCase(
            repository, storage, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUploadedFileSource_DeletesItsStorageObject()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateUploadedFileAnalysis(ownerUserId, "owner/abc123");
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new DeleteJobAnalysisUseCase(
            repository, storage, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Equal(["owner/abc123"], storage.DeletedKeys);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageDeleteFails_StillReportsSuccess()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage { ExceptionToThrowOnDelete = new HttpRequestException("Storage is unreachable") };
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateUploadedFileAnalysis(ownerUserId, "owner/abc123");
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = new DeleteJobAnalysisUseCase(
            repository, storage, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Empty(repository.Analyses);
    }
}
