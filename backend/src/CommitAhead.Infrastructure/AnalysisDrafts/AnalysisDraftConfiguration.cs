using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

public sealed class AnalysisDraftConfiguration : IEntityTypeConfiguration<AnalysisDraft>
{
    public void Configure(EntityTypeBuilder<AnalysisDraft> builder)
    {
        builder.ToTable("analysis_drafts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(d => d.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // No FK — polymorphic, same reasoning as EvidenceLink's own sourceType/sourceId columns
        // (model.md, "cross-aggregate references"): validated by whichever use case creates the
        // draft, not enforceable as a single database FK across three possible target tables.
        builder.Property(d => d.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(d => d.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(d => d.AppliedAtUtc)
            .HasColumnName("applied_at_utc");

        builder.Property(d => d.DiscardedAtUtc)
            .HasColumnName("discarded_at_utc");

        // Cascade, not Restrict: proposals have no lifecycle independent of their owning draft —
        // same reasoning as JobAnalysis's own Requirements/Gaps.
        builder.HasMany(d => d.SuggestionProposals).WithOne().HasForeignKey("AnalysisDraftId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.LinkProposals).WithOne().HasForeignKey("AnalysisDraftId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.StudyItemProposals).WithOne().HasForeignKey("AnalysisDraftId").OnDelete(DeleteBehavior.Cascade);

        // At most one Pending AnalysisDraft per (SourceType, SourceId) — model.md invariant,
        // enforced here at the database level (a partial unique index over Pending rows only) since
        // this aggregate has no way to see other AnalysisDraft instances for the same source to
        // check itself. The filter text matches HasConversion<string>()'s serialized enum name.
        builder.HasIndex(d => new { d.SourceType, d.SourceId })
            .IsUnique()
            .HasFilter("status = 'Pending'");

        builder.HasIndex(d => d.OwnerUserId);
    }
}
