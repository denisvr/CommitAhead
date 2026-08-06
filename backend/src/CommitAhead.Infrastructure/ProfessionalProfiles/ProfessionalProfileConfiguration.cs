using CommitAhead.Domain.Identity;
using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class ProfessionalProfileConfiguration : IEntityTypeConfiguration<ProfessionalProfile>
{
    public void Configure(EntityTypeBuilder<ProfessionalProfile> builder)
    {
        builder.ToTable("professional_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User). Restrict,
        // not cascade: this app has no user-deletion use case, matching every other aggregate.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A singleton per owner (model.md) — the domain layer already enforces this via
        // CreateProfessionalProfileUseCase's existence check; the unique index is the DB-level
        // backup against a concurrent double-create race.
        builder.HasIndex(p => p.OwnerUserId).IsUnique();

        // A single jsonb column via ContactInfoValueConverter, not OwnsOne/ComplexProperty:
        // ContactInfo is constructor-only, and EF cannot constructor-bind a containing entity's
        // parameter to a nested owned/complex sub-object (see ContactInfoValueConverter's comment).
        builder.Property(p => p.ContactInfo)
            .HasColumnName("contact_info")
            .HasConversion(new ContactInfoValueConverter())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(p => p.SummaryMarkdown)
            .HasColumnName("summary_markdown")
            .HasMaxLength(ValidationLimits.MarkdownMaxLength)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // Cascade, not Restrict: unlike StudyItem.Reviews (protected by a hard-delete guard
        // invariant), nothing protects these children from their own aggregate root's deletion —
        // and deleting a ProfessionalProfile isn't an MVP use case at all (ADR-0012).
        builder.HasMany(p => p.Experience).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Education).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Skills).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Languages).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Certifications).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Projects).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.ProfileLinks).WithOne().HasForeignKey("ProfessionalProfileId").OnDelete(DeleteBehavior.Cascade);
    }
}
