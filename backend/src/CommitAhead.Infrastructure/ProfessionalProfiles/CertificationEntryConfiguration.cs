using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class CertificationEntryConfiguration : IEntityTypeConfiguration<CertificationEntry>
{
    public void Configure(EntityTypeBuilder<CertificationEntry> builder)
    {
        builder.ToTable("certification_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        // Stamped by ProfessionalProfile.ReplaceCertifications from the caller's array order —
        // see ProfessionalProfileRepository's ordered Include for the read side.
        builder.Property(e => e.Position).HasColumnName("position").IsRequired();

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.IssuingOrganisation).HasColumnName("issuing_organisation").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(e => e.CredentialId).HasColumnName("credential_id").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(e => e.Url).HasColumnName("url").HasMaxLength(ValidationLimits.UrlMaxLength);

        // See ExperienceEntryConfiguration/YearMonthConversion for why this is one converted int
        // column, not two (constructor-bound YearMonth).
        builder.Property(e => e.IssuedAt).HasColumnName("issued_at").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasConversion(v => YearMonthConversion.ToNullableInt(v), v => YearMonthConversion.FromNullableInt(v));
    }
}
