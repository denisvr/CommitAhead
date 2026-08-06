using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class ProfileLinkConfiguration : IEntityTypeConfiguration<ProfileLink>
{
    public void Configure(EntityTypeBuilder<ProfileLink> builder)
    {
        builder.ToTable("profile_links");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("ProfessionalProfileId")
            .HasColumnName("professional_profile_id");

        builder.Property(l => l.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(l => l.Label).HasColumnName("label").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(l => l.Url).HasColumnName("url").HasMaxLength(ValidationLimits.UrlMaxLength).IsRequired();
    }
}
