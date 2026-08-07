using CommitAhead.Domain.JobAnalyses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.JobAnalyses;

public sealed class JobGapConfiguration : IEntityTypeConfiguration<JobGap>
{
    public void Configure(EntityTypeBuilder<JobGap> builder)
    {
        builder.ToTable("job_gaps");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("JobAnalysisId")
            .HasColumnName("job_analysis_id");

        builder.Property(g => g.RequirementId).HasColumnName("requirement_id").IsRequired();
        builder.Property(g => g.MatchLevel).HasColumnName("match_level").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(g => g.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(g => g.Rationale).HasColumnName("rationale").HasMaxLength(ValidationLimits.GapRationaleMaxLength).IsRequired();

        // No FK from RequirementId to job_requirements — same reasoning as EvidenceLink's
        // polymorphic sourceType/sourceId columns having no database FK: invariant 16 (a gap's
        // RequirementId must reference a requirement on the same JobAnalysis) is already fully
        // enforced in memory by JobAnalysis.AddGap/RemoveRequirement before either collection is
        // ever persisted, and both types are children of the same aggregate root — there is no
        // cross-aggregate gap this index needs to close, just a lookup helper.
        builder.HasIndex(g => g.RequirementId);
    }
}
