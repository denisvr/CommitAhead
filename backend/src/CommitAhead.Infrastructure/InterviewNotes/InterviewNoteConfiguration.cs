using CommitAhead.Domain.Identity;
using CommitAhead.Domain.InterviewNotes;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValidationLimits = CommitAhead.Domain.InterviewNotes.ValidationLimits;

namespace CommitAhead.Infrastructure.InterviewNotes;

public sealed class InterviewNoteConfiguration : IEntityTypeConfiguration<InterviewNote>
{
    public void Configure(EntityTypeBuilder<InterviewNote> builder)
    {
        builder.ToTable("interview_notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User). Restrict,
        // matching every other user-owned aggregate's OwnerUserId FK.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(n => n.Company).HasColumnName("company").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(n => n.Role).HasColumnName("role").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(n => n.InterviewRound).HasColumnName("interview_round").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(n => n.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(n => n.OtherLabel).HasColumnName("other_label").HasMaxLength(ValidationLimits.ShortTextMaxLength);

        // DateOnly has first-class Npgsql/EF Core 10 support as `date` — no converter needed,
        // unlike ProfessionalProfiles' YearMonth (a custom value object with no built-in mapping).
        builder.Property(n => n.Date).HasColumnName("date").IsRequired();

        // TEXT[] via Npgsql, same technique as StudyItem.Tags.
        builder.Property(n => n.Questions).HasColumnName("questions").IsRequired();
        builder.Property(n => n.Gaps).HasColumnName("gaps").IsRequired();
        builder.Property(n => n.Lessons).HasColumnName("lessons").IsRequired();

        builder.Property(n => n.JobAnalysisId).HasColumnName("job_analysis_id");

        builder.Property(n => n.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(n => n.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        // Single-column FK, deliberately NOT the composite (JobAnalysisId, OwnerUserId) pattern
        // CVPresentationConfiguration uses against ProfessionalProfile: a composite FK's ON DELETE
        // SET NULL nulls every column in the FK, which would also null this row's own OwnerUserId —
        // wrong, since OwnerUserId is InterviewNote's own identity, not part of the cross-aggregate
        // reference being cleared. This FK is what enforces invariant 19 (preserve the note, null
        // the reference); invariant 29 (same-owner) stays an application-level check
        // (Create/UpdateInterviewNoteUseCase), same as every other cross-aggregate reference this
        // app has before a DB-level same-owner guarantee is feasible for it.
        builder.HasOne<JobAnalysis>()
            .WithMany()
            .HasForeignKey(n => n.JobAnalysisId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.OwnerUserId);
        builder.HasIndex(n => n.JobAnalysisId);
    }
}
