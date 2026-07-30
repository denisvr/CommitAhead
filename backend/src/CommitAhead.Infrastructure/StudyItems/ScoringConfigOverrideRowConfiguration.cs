using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.StudyItems;

internal sealed class ScoringConfigOverrideRowConfiguration : IEntityTypeConfiguration<ScoringConfigOverrideRow>
{
    public void Configure(EntityTypeBuilder<ScoringConfigOverrideRow> builder)
    {
        builder.ToTable("scoring_config_overrides");

        builder.HasKey(row => row.OwnerUserId);

        builder.Property(row => row.OwnerUserId)
            .HasColumnName("owner_user_id");

        builder.Property(row => row.ImportanceWeight)
            .HasColumnName("importance_weight")
            .IsRequired();

        builder.Property(row => row.DemandWeight)
            .HasColumnName("demand_weight")
            .IsRequired();

        builder.Property(row => row.MasteryGapWeight)
            .HasColumnName("mastery_gap_weight")
            .IsRequired();
    }
}
