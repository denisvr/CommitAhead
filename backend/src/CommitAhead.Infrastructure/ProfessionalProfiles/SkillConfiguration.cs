using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("skills");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        builder.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(s => s.NormalizedKey).HasColumnName("normalized_key").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(s => s.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.Proficiency).HasColumnName("proficiency").HasConversion<string>().HasMaxLength(32);

        // Domain already enforces this (ProfessionalProfile.ReplaceSkills, invariant 20) — the
        // index is the DB-level backup.
        builder.HasIndex("ProfessionalProfileId", nameof(Skill.NormalizedKey)).IsUnique();
    }
}
