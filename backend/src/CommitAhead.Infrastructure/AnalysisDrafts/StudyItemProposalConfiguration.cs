using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Infrastructure.StudyItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

public sealed class StudyItemProposalConfiguration : IEntityTypeConfiguration<StudyItemProposal>
{
    public void Configure(EntityTypeBuilder<StudyItemProposal> builder)
    {
        builder.ToTable("study_item_proposals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("AnalysisDraftId")
            .HasColumnName("analysis_draft_id");

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.ProposedTitle)
            .HasColumnName("proposed_title")
            .HasMaxLength(CommitAhead.Domain.StudyItems.ValidationLimits.TitleMaxLength)
            .IsRequired();

        builder.Property(p => p.ProposedCategory)
            .HasColumnName("proposed_category")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.ProposedDetails)
            .HasColumnName("proposed_details")
            .HasConversion(new StudyItemDetailsValueConverter())
            .HasColumnType("jsonb")
            .IsRequired();

        // TEXT[] via Npgsql — same as StudyItem.Tags.
        builder.Property(p => p.ProposedTags)
            .HasColumnName("proposed_tags")
            .IsRequired();

        builder.Property(p => p.ProposedImportance)
            .HasColumnName("proposed_importance")
            .IsRequired();

        builder.Property(p => p.AcceptedTitle)
            .HasColumnName("accepted_title")
            .HasMaxLength(CommitAhead.Domain.StudyItems.ValidationLimits.TitleMaxLength);

        builder.Property(p => p.AcceptedCategory)
            .HasColumnName("accepted_category")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.AcceptedDetails)
            .HasColumnName("accepted_details")
            .HasConversion(new NullableStudyItemDetailsValueConverter())
            .HasColumnType("jsonb");

        builder.Property(p => p.AcceptedTags)
            .HasColumnName("accepted_tags");

        builder.Property(p => p.AcceptedImportance)
            .HasColumnName("accepted_importance");

        builder.Property(p => p.AcceptedInitialMastery)
            .HasColumnName("accepted_initial_mastery");
    }
}
