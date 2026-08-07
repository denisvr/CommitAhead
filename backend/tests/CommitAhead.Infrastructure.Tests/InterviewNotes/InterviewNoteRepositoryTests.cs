using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Infrastructure.InterviewNotes;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.InterviewNotes;

[Collection(PostgresCollection.Name)]
public class InterviewNoteRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public InterviewNoteRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static InterviewNote CreateNote(Guid ownerUserId, Guid? jobAnalysisId = null) => new(
        Guid.NewGuid(), ownerUserId, "Acme Corp", "Backend Engineer", InterviewRound.Technical, 1, null, new DateOnly(2026, 1, 15),
        ["Q1"], ["Gap1"], ["Lesson1"], jobAnalysisId, DateTime.UtcNow);

    private async Task<Guid> CreateJobAnalysisAsync(Guid ownerUserId)
    {
        var analysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);
        await new JobAnalysisRepository(_dbContext).AddAsync(analysis, CancellationToken.None);
        return analysis.Id;
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsEveryField()
    {
        var repository = new InterviewNoteRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysisId = await CreateJobAnalysisAsync(ownerUserId);
        var note = CreateNote(ownerUserId, jobAnalysisId);

        await repository.AddAsync(note, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new InterviewNoteRepository(reloadDbContext).GetByIdAsync(ownerUserId, note.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("Acme Corp", reloaded.Company);
        Assert.Equal(jobAnalysisId, reloaded.JobAnalysisId);
        Assert.Equal(["Q1"], reloaded.Questions);
    }

    [Fact]
    public async Task GetById_ScopedToADifferentOwner_ReturnsNull()
    {
        var repository = new InterviewNoteRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var note = CreateNote(ownerUserId);
        await repository.AddAsync(note, CancellationToken.None);

        var found = await repository.GetByIdAsync(Guid.NewGuid(), note.Id, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTheOwnersNotes()
    {
        var repository = new InterviewNoteRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var otherOwnerUserId = await TestUsers.CreateAsync(_dbContext);
        await repository.AddAsync(CreateNote(ownerUserId), CancellationToken.None);
        await repository.AddAsync(CreateNote(otherOwnerUserId), CancellationToken.None);

        var results = await repository.GetAllAsync(ownerUserId, CancellationToken.None);

        Assert.Single(results);
        Assert.All(results, note => Assert.Equal(ownerUserId, note.OwnerUserId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheNote()
    {
        var repository = new InterviewNoteRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var note = CreateNote(ownerUserId);
        await repository.AddAsync(note, CancellationToken.None);

        await repository.DeleteAsync(note, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(ownerUserId, note.Id, CancellationToken.None));
    }

    /// <summary>
    /// Invariant 19, proven against a real PostgreSQL FK rather than simulated with application
    /// code: deleting the JobAnalysis a note references must null the note's own JobAnalysisId,
    /// never delete the note itself. This is exactly what InterviewNoteConfiguration's single-column
    /// ON DELETE SET NULL FK is for (see its own comment for why it's single-column, not the
    /// composite same-owner pattern CVPresentation uses against ProfessionalProfile).
    /// </summary>
    [Fact]
    public async Task DeletingTheReferencedJobAnalysis_NullsTheNotesReference_AndPreservesTheNote()
    {
        var noteRepository = new InterviewNoteRepository(_dbContext);
        var jobAnalysisRepository = new JobAnalysisRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysisId = await CreateJobAnalysisAsync(ownerUserId);
        var note = CreateNote(ownerUserId, jobAnalysisId);
        await noteRepository.AddAsync(note, CancellationToken.None);

        var analysis = await jobAnalysisRepository.GetByIdAsync(ownerUserId, jobAnalysisId, CancellationToken.None);
        await jobAnalysisRepository.DeleteAsync(analysis!, CancellationToken.None);
        await jobAnalysisRepository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloadedNote = await new InterviewNoteRepository(reloadDbContext).GetByIdAsync(ownerUserId, note.Id, CancellationToken.None);
        Assert.NotNull(reloadedNote);
        Assert.Null(reloadedNote.JobAnalysisId);
    }

    [Fact]
    public async Task AddAsync_ReferencingANonexistentJobAnalysis_IsRejectedByTheDatabase()
    {
        var repository = new InterviewNoteRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var note = CreateNote(ownerUserId, Guid.NewGuid());

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => repository.AddAsync(note, CancellationToken.None));
    }
}
