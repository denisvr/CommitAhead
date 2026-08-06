using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class LanguageEntryConfiguration : IEntityTypeConfiguration<LanguageEntry>
{
    public void Configure(EntityTypeBuilder<LanguageEntry> builder)
    {
        builder.ToTable("language_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        builder.Property(e => e.Language).HasColumnName("language").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.Proficiency).HasColumnName("proficiency").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Certification).HasColumnName("certification").HasMaxLength(ValidationLimits.ShortTextMaxLength);
    }
}
