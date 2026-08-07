using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.AIUsage;

public sealed class AIUsageRecordConfiguration : IEntityTypeConfiguration<AIUsageRecord>
{
    public void Configure(EntityTypeBuilder<AIUsageRecord> builder)
    {
        builder.ToTable("ai_usage_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: OwnerUserId references User).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(ValidationLimits.IdempotencyKeyMaxLength)
            .IsRequired();

        // ADR-0014: durable idempotency is a real unique constraint, not just an application-level
        // check — a repeated key at the database level fails loudly rather than double-charging.
        builder.HasIndex(r => r.IdempotencyKey).IsUnique();

        builder.Property(r => r.CommandType)
            .HasColumnName("command_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // No FK — polymorphic, same reasoning as AnalysisDraft/EvidenceLink's own sourceType/sourceId.
        builder.Property(r => r.SourceId)
            .HasColumnName("source_id")
            .IsRequired();

        builder.Property(r => r.Provider)
            .HasColumnName("provider")
            .HasMaxLength(ValidationLimits.ProviderMaxLength)
            .IsRequired();

        builder.Property(r => r.Model)
            .HasColumnName("model")
            .HasMaxLength(ValidationLimits.ModelMaxLength)
            .IsRequired();

        builder.Property(r => r.PricingVersion)
            .HasColumnName("pricing_version")
            .HasMaxLength(ValidationLimits.PricingVersionMaxLength)
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasColumnName("currency")
            .HasMaxLength(ValidationLimits.CurrencyCodeLength)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.ReservedInputTokens)
            .HasColumnName("reserved_input_tokens")
            .IsRequired();

        builder.Property(r => r.ReservedOutputTokens)
            .HasColumnName("reserved_output_tokens")
            .IsRequired();

        builder.Property(r => r.ReservedCost)
            .HasColumnName("reserved_cost")
            .IsRequired();

        builder.Property(r => r.ActualInputTokens)
            .HasColumnName("actual_input_tokens");

        builder.Property(r => r.ActualOutputTokens)
            .HasColumnName("actual_output_tokens");

        builder.Property(r => r.ActualCost)
            .HasColumnName("actual_cost");

        // No FK to analysis_drafts: intentionally, so a completed usage record survives even if the
        // AnalysisDraft it points to is ever deleted — this is an audit pointer, not a real
        // relationship AnalysisDraft's own lifecycle should govern.
        builder.Property(r => r.AnalysisDraftId)
            .HasColumnName("analysis_draft_id");

        builder.Property(r => r.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(r => r.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(r => r.OutcomeCode)
            .HasColumnName("outcome_code")
            .HasMaxLength(ValidationLimits.OutcomeCodeMaxLength);

        // The budget-check query (GetSpentCostAsync) filters by owner + a time window + Status.
        builder.HasIndex(r => new { r.OwnerUserId, r.StartedAtUtc });
    }
}
