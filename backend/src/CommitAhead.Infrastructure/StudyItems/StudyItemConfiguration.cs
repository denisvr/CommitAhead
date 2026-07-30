using CommitAhead.Domain.StudyItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.StudyItems;

public sealed class StudyItemConfiguration : IEntityTypeConfiguration<StudyItem>
{
    public void Configure(EntityTypeBuilder<StudyItem> builder)
    {
        builder.ToTable("study_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(item => item.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Importance)
            .HasColumnName("importance")
            .IsRequired();

        builder.Property(item => item.InitialMastery)
            .HasColumnName("initial_mastery")
            .IsRequired();

        // TEXT[] via Npgsql (docs/architecture/persistence.md, "Tags").
        builder.Property(item => item.Tags)
            .HasColumnName("tags")
            .IsRequired();

        // jsonb, self-describing "kind" discriminator (docs/architecture/persistence.md, "Typed
        // category details") — the sibling `category` column above is the domain-level
        // discriminator (invariant 6), not something this converter can read.
        builder.Property(item => item.Details)
            .HasColumnName("details")
            .HasConversion(new StudyItemDetailsValueConverter())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(item => item.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // Optional owned value object — EF Core treats it as null when every mapped column is
        // null, and never invokes PriorityOverride's constructor in that case.
        builder.OwnsOne(item => item.PriorityOverride, priorityOverride =>
        {
            priorityOverride.Property(p => p.Score).HasColumnName("priority_override_score");
            priorityOverride.Property(p => p.Reason).HasColumnName("priority_override_reason").HasMaxLength(500);
        });

        // StudyReview has no public back-reference to its owning StudyItem (model.md describes
        // it as a one-directional child collection) — StudyItemId is a shadow FK. Reviews is a
        // read-only computed property (=> _reviews), so EF's backing-field convention resolves
        // the navigation straight to the private "_reviews" list for both reads and writes.
        builder.HasMany(item => item.Reviews)
            .WithOne()
            .HasForeignKey("StudyItemId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.OwnerUserId, item.Status });
    }
}
