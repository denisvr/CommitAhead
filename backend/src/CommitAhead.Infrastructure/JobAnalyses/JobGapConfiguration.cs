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

        // Composite FK to JobRequirement's (Id, JobAnalysisId) alternate key — not just an index —
        // so PostgreSQL itself rejects a gap referencing a requirement from a different
        // JobAnalysis, as defense-in-depth alongside the in-memory invariant already enforced by
        // JobAnalysis.AddGap/RemoveRequirement. Restrict, not Cascade: JobGap already cascades
        // directly from JobAnalysis (below) — deleting a JobAnalysis removes both children via that
        // shorter path in the same SaveChanges, so this FK never blocks a real deletion; it only
        // rejects the invalid write this invariant exists to prevent. No navigation property either
        // side, same as EvidenceLink's polymorphic sourceType/sourceId columns.
        builder.HasOne<JobRequirement>()
            .WithMany()
            .HasForeignKey("RequirementId", "JobAnalysisId")
            .HasPrincipalKey(nameof(JobRequirement.Id), "JobAnalysisId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
