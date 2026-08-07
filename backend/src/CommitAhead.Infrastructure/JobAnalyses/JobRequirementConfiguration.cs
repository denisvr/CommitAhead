using CommitAhead.Domain.JobAnalyses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.JobAnalyses;

public sealed class JobRequirementConfiguration : IEntityTypeConfiguration<JobRequirement>
{
    public void Configure(EntityTypeBuilder<JobRequirement> builder)
    {
        builder.ToTable("job_requirements");

        builder.HasKey(r => r.Id);

        // ValueGeneratedNever: Id is always app-assigned (JobRequirement's constructor), matching
        // ExperienceEntry's own shadow-FK child mapping.
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("JobAnalysisId")
            .HasColumnName("job_analysis_id");

        builder.Property(r => r.Text).HasColumnName("text").HasMaxLength(ValidationLimits.RequirementTextMaxLength).IsRequired();
        builder.Property(r => r.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.SourceExcerpt).HasColumnName("source_excerpt").HasMaxLength(ValidationLimits.SourceExcerptMaxLength).IsRequired();
    }
}
