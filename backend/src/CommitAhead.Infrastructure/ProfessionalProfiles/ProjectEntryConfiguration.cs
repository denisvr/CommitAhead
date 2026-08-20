using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class ProjectEntryConfiguration : IEntityTypeConfiguration<ProjectEntry>
{
    public void Configure(EntityTypeBuilder<ProjectEntry> builder)
    {
        builder.ToTable("project_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        // Stamped by ProfessionalProfile.ReplaceProjects from the caller's array order — see
        // ProfessionalProfileRepository's ordered Include for the read side.
        builder.Property(e => e.Position).HasColumnName("position").IsRequired();

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.DescriptionMarkdown).HasColumnName("description_markdown").HasMaxLength(ValidationLimits.MarkdownMaxLength).IsRequired();
        builder.Property(e => e.Url).HasColumnName("url").HasMaxLength(ValidationLimits.UrlMaxLength);

        // See ExperienceEntryConfiguration for why this is a plain uuid[] rather than a join table.
        builder.Property(e => e.SkillIds).HasColumnName("skill_ids").IsRequired();

        // See ExperienceEntryConfiguration/YearMonthConversion for why this is one converted int
        // column, not two (constructor-bound YearMonth).
        builder.Property(e => e.StartDate).HasColumnName("start_date").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
        builder.Property(e => e.EndDate).HasColumnName("end_date").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
    }
}
