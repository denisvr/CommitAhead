using CommitAhead.Domain.AnalysisDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

public sealed class LinkProposalConfiguration : IEntityTypeConfiguration<LinkProposal>
{
    public void Configure(EntityTypeBuilder<LinkProposal> builder)
    {
        builder.ToTable("link_proposals");

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

        // No FK to StudyItem: this is a proposed, not-yet-real link — its cross-owner safety is
        // validated for real only when an accepted LinkProposal becomes a genuine EvidenceLink
        // (EvidenceLinkConfiguration's own composite same-owner FK), which is the actual security
        // boundary. Indexed since the analyzing use case looks proposals up by target.
        builder.Property(p => p.TargetStudyItemId)
            .HasColumnName("target_study_item_id")
            .IsRequired();

        builder.Property(p => p.ProposedWeight)
            .HasColumnName("proposed_weight")
            .IsRequired();

        builder.Property(p => p.ProposedRationale)
            .HasColumnName("proposed_rationale")
            .HasMaxLength(ValidationLimits.LinkProposalRationaleMaxLength)
            .IsRequired();

        builder.Property(p => p.AcceptedWeight)
            .HasColumnName("accepted_weight");

        builder.Property(p => p.AcceptedRationale)
            .HasColumnName("accepted_rationale")
            .HasMaxLength(ValidationLimits.LinkProposalRationaleMaxLength);

        builder.HasIndex(p => p.TargetStudyItemId);
    }
}
