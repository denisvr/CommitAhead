using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.Identity;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Persistence;

public sealed class CommitAheadDbContext : DbContext
{
    public CommitAheadDbContext(DbContextOptions<CommitAheadDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();

    public DbSet<CVPresentation> CVPresentations => Set<CVPresentation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
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
