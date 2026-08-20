using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class ExperienceEntryConfiguration : IEntityTypeConfiguration<ExperienceEntry>
{
    public void Configure(EntityTypeBuilder<ExperienceEntry> builder)
    {
        builder.ToTable("experience_entries");

        builder.HasKey(e => e.Id);

        // ValueGeneratedNever: Id is always app-assigned (ExperienceEntry's constructor), matching
        // StudyReview's own shadow-FK child mapping.
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        // Stamped by ProfessionalProfile.ReplaceExperience from the caller's array order — see
        // ProfessionalProfileRepository's ordered Include for the read side.
        builder.Property(e => e.Position).HasColumnName("position").IsRequired();

        builder.Property(e => e.Company).HasColumnName("company").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.Client).HasColumnName("client").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.EmploymentType).HasColumnName("employment_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.WorkMode).HasColumnName("work_mode").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.SummaryMarkdown).HasColumnName("summary_markdown").HasMaxLength(ValidationLimits.MarkdownMaxLength).IsRequired();

        // TEXT[]/UUID[] via Npgsql, same technique as StudyItem.Tags (docs/architecture/persistence.md).
        builder.Property(e => e.Achievements).HasColumnName("achievements").IsRequired();

        // A plain uuid[] rather than a second FK-backed join table — see this slice's plan for why
        // (ExperienceEntry has no Skill-entity navigation to bind a many-to-many through; the
        // referenced-Skill-exists and Skill-can't-be-removed-while-referenced invariants are
        // already fully enforced in ProfessionalProfile itself).
        builder.Property(e => e.SkillIds).HasColumnName("skill_ids").IsRequired();

        // A single converted int column each (see YearMonthConversion) — YearMonth is
        // constructor-only, and EF cannot constructor-bind a containing entity's parameter to a
        // nested owned/complex sub-object.
        builder.Property(e => e.StartDate).HasColumnName("start_date").HasConversion(v => YearMonthConversion.ToInt(v), v => YearMonthConversion.FromInt(v)).IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("end_date").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
    }
}
