using CommitAhead.Domain.AnalysisDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

public sealed class SuggestionProposalConfiguration : IEntityTypeConfiguration<SuggestionProposal>
{
    public void Configure(EntityTypeBuilder<SuggestionProposal> builder)
    {
        builder.ToTable("suggestion_proposals");

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

        // jsonb, self-describing "kind" discriminator (StructuredSuggestion vs AdvisorySuggestion)
        // — same pattern as JobSource/StudyItemDetails. The same nullable-typed converter maps
        // both this (never actually null) and AcceptedPayload (null until Accept).
        builder.Property(p => p.ProposedPayload)
            .HasColumnName("proposed_payload")
            .HasConversion(new SuggestionPayloadValueConverter())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(p => p.AcceptedPayload)
            .HasColumnName("accepted_payload")
            .HasConversion(new NullableSuggestionPayloadValueConverter())
            .HasColumnType("jsonb");
    }
}
