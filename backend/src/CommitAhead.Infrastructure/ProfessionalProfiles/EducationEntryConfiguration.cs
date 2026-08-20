using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class EducationEntryConfiguration : IEntityTypeConfiguration<EducationEntry>
{
    public void Configure(EntityTypeBuilder<EducationEntry> builder)
    {
        builder.ToTable("education_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        // Stamped by ProfessionalProfile.ReplaceEducation from the caller's array order — see
        // ProfessionalProfileRepository's ordered Include for the read side.
        builder.Property(e => e.Position).HasColumnName("position").IsRequired();

        builder.Property(e => e.Institution).HasColumnName("institution").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.Degree).HasColumnName("degree").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.Field).HasColumnName("field").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.DetailsMarkdown).HasColumnName("details_markdown").HasMaxLength(ValidationLimits.MarkdownMaxLength);

        // See ExperienceEntryConfiguration/YearMonthConversion for why this is one converted int
        // column, not two (constructor-bound YearMonth).
        builder.Property(e => e.StartDate).HasColumnName("start_date").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
        builder.Property(e => e.EndDate).HasColumnName("end_date").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
    }
}
