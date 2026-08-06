using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.Identity;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.StudyItems;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Persistence;

public sealed class CommitAheadDbContext : DbContext
{
    public CommitAheadDbContext(DbContextOptions<CommitAheadDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<StudyItem> StudyItems => Set<StudyItem>();

    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();

    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();

    public DbSet<CVPresentation> CVPresentations => Set<CVPresentation>();

    internal DbSet<ScoringConfigOverrideRow> ScoringConfigOverrides => Set<ScoringConfigOverrideRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new StudyItemConfiguration());
        modelBuilder.ApplyConfiguration(new StudyReviewConfiguration());
        modelBuilder.ApplyConfiguration(new ScoringConfigOverrideRowConfiguration());
        modelBuilder.ApplyConfiguration(new EvidenceLinkConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessionalProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ExperienceEntryConfiguration());
        modelBuilder.ApplyConfiguration(new EducationEntryConfiguration());
        modelBuilder.ApplyConfiguration(new SkillConfiguration());
        modelBuilder.ApplyConfiguration(new LanguageEntryConfiguration());
        modelBuilder.ApplyConfiguration(new CertificationEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileLinkConfiguration());
        modelBuilder.ApplyConfiguration(new CVPresentationConfiguration());
    }
}
