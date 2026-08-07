using CommitAhead.Domain.Identity;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.JobAnalyses;

public sealed class JobAnalysisConfiguration : IEntityTypeConfiguration<JobAnalysis>
{
    public void Configure(EntityTypeBuilder<JobAnalysis> builder)
    {
        builder.ToTable("job_analyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User). Restrict,
        // matching every other user-owned aggregate's OwnerUserId FK.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(ValidationLimits.TitleMaxLength)
            .IsRequired();

        // jsonb, self-describing "kind" discriminator — mirrors StudyItem.Details. JobSource has
        // only two variants (PastedText/UploadedFile) and no sibling discriminator column of its
        // own, unlike StudyItem.Category.
        builder.Property(a => a.JobSource)
            .HasColumnName("job_source")
            .HasConversion(new JobSourceValueConverter())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(a => a.NotesMarkdown)
            .HasColumnName("notes_markdown")
            .HasMaxLength(ValidationLimits.NotesMarkdownMaxLength);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // Cascade, not Restrict: Requirements/Gaps have no lifecycle independent of their owning
        // JobAnalysis (JobAnalysis.cs's own comment — they arrive one accepted-proposal at a time
        // from Phase 4's pipeline, never created/loaded standalone), unlike StudyReview's
        // protective Restrict on StudyItem.
        builder.HasMany(a => a.Requirements).WithOne().HasForeignKey("JobAnalysisId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Gaps).WithOne().HasForeignKey("JobAnalysisId").OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.OwnerUserId);
    }
}
