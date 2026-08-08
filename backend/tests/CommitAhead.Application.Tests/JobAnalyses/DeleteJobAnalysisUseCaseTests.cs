using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.AnalysisDrafts;
using CommitAhead.Application.Tests.Auth;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.JobAnalyses;

public class DeleteJobAnalysisUseCaseTests
{
    private static JobAnalysis CreatePastedTextAnalysis(Guid ownerUserId) => new(
        Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);

    private static JobAnalysis CreateUploadedFileAnalysis(Guid ownerUserId, string storageObjectKey) => new(
        Guid.NewGuid(), ownerUserId, "Title", new UploadedFile(storageObjectKey, "posting.pdf", "application/pdf", "Extracted text."), null, DateTime.UtcNow);

    private static DeleteJobAnalysisUseCase CreateUseCase(
        FakeJobAnalysisRepository repository, FakeJobPostingStorage storage, Guid ownerUserId,
        FakeEvidenceLinkRepository? evidenceLinkRepository = null, FakeAnalysisDraftRepository? analysisDraftRepository = null, ILogger<DeleteJobAnalysisUseCase>? logger = null) =>
        new(
            repository, evidenceLinkRepository ?? new FakeEvidenceLinkRepository(), analysisDraftRepository ?? new FakeAnalysisDraftRepository(), new FakeUnitOfWork(),
            storage, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" }, logger ?? NullLogger<DeleteJobAnalysisUseCase>.Instance);

    [Fact]
    public async Task ExecuteAsync_WithAnExistingAnalysis_DeletesItAndReturnsSuccess()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreatePastedTextAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);
        var useCase = CreateUseCase(repository, storage, ownerUserId);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExistingAnalysis_ReturnsNotFound()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var useCase = CreateUseCase(repository, storage, Guid.NewGuid());

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
        var useCase = CreateUseCase(repository, storage, ownerUserId);

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
        var useCase = CreateUseCase(repository, storage, ownerUserId);

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
        var useCase = CreateUseCase(repository, storage, ownerUserId);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Empty(repository.Analyses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageDeleteFails_LogsTheStorageObjectKey_NeverTheException()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage { ExceptionToThrowOnDelete = new HttpRequestException("Storage is unreachable") };
        var ownerUserId = Guid.NewGuid();
        var analysis = CreateUploadedFileAnalysis(ownerUserId, "owner/abc123");
        await repository.AddAsync(analysis, CancellationToken.None);
        var logger = new RecordingLogger<DeleteJobAnalysisUseCase>();
        var useCase = CreateUseCase(repository, storage, ownerUserId, logger: logger);

        await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("owner/abc123", entry.Message, StringComparison.Ordinal);
    }

    /// <summary>ADR-0011: deleting the source must also remove its EvidenceLinks and AnalysisDrafts, leaving unrelated ones untouched.</summary>
    [Fact]
    public async Task ExecuteAsync_DeletesEvidenceLinksAndAnalysisDraftsForThisSource_ButLeavesOthersUntouched()
    {
        var repository = new FakeJobAnalysisRepository();
        var storage = new FakeJobPostingStorage();
        var ownerUserId = Guid.NewGuid();
        var analysis = CreatePastedTextAnalysis(ownerUserId);
        await repository.AddAsync(analysis, CancellationToken.None);

        var otherJobAnalysisId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var evidenceLinkRepository = new FakeEvidenceLinkRepository();
        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, analysis.Id, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow), CancellationToken.None);
        var otherSourceLink = new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, otherJobAnalysisId, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow);
        await evidenceLinkRepository.AddAsync(otherSourceLink, CancellationToken.None);
        var otherOwnerLink = new EvidenceLink(Guid.NewGuid(), otherOwnerId, EvidenceSourceType.JobAnalysis, analysis.Id, Guid.NewGuid(), 3, "Matches.", DateTime.UtcNow);
        await evidenceLinkRepository.AddAsync(otherOwnerLink, CancellationToken.None);

        var analysisDraftRepository = new FakeAnalysisDraftRepository();
        await analysisDraftRepository.AddAsync(
            new Domain.AnalysisDrafts.AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, analysis.Id, [], [], [], DateTime.UtcNow), CancellationToken.None);
        var otherSourceDraft = new Domain.AnalysisDrafts.AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, otherJobAnalysisId, [], [], [], DateTime.UtcNow);
        await analysisDraftRepository.AddAsync(otherSourceDraft, CancellationToken.None);

        var useCase = CreateUseCase(repository, storage, ownerUserId, evidenceLinkRepository, analysisDraftRepository);

        var result = await useCase.ExecuteAsync(analysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);
        Assert.Equal([otherSourceLink, otherOwnerLink], evidenceLinkRepository.Links);
        Assert.Equal([otherSourceDraft], analysisDraftRepository.Drafts);
    }
}
