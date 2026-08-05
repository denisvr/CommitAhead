using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.Identity;
using CommitAhead.Domain.StudyItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.EvidenceLinks;

public sealed class EvidenceLinkConfiguration : IEntityTypeConfiguration<EvidenceLink>
{
    public void Configure(EntityTypeBuilder<EvidenceLink> builder)
    {
        builder.ToTable("evidence_links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(link => link.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(link => link.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(link => link.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(link => link.TargetStudyItemId)
            .HasColumnName("target_study_item_id")
            .IsRequired();

        builder.Property(link => link.Weight)
            .HasColumnName("weight")
            .IsRequired();

        builder.Property(link => link.Rationale)
            .HasColumnName("rationale")
            .HasMaxLength(ValidationLimits.EvidenceLinkRationaleMaxLength)
            .IsRequired();

        builder.Property(link => link.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite FK to StudyItem's (owner_user_id, id) alternate key — not just
        // target_study_item_id -> study_items.id. This is the database-level "same owner"
        // guarantee model.md invariant 29 asks for: a link's own owner_user_id must match the
        // StudyItem it targets, so cross-owner linking is impossible even if application code is
        // bypassed. No cascade: a StudyItem cannot be hard-deleted while any EvidenceLink still
        // targets it (invariant 2) even if the application-level guard were ever bypassed. The
        // sourceType/sourceId side is polymorphic and has no database FK (model.md, "cross-aggregate
        // references"); it is validated by whatever use case creates the link.
        builder.HasOne<StudyItem>()
            .WithMany()
            .HasForeignKey(link => new { link.OwnerUserId, link.TargetStudyItemId })
            .HasPrincipalKey(item => new { item.OwnerUserId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => new { link.SourceType, link.SourceId, link.TargetStudyItemId })
            .IsUnique();

        builder.HasIndex(link => link.TargetStudyItemId);
    }
}
