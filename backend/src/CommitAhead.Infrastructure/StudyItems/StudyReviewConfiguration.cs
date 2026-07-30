using CommitAhead.Domain.StudyItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.StudyItems;

public sealed class StudyReviewConfiguration : IEntityTypeConfiguration<StudyReview>
{
    public void Configure(EntityTypeBuilder<StudyReview> builder)
    {
        builder.ToTable("study_reviews");

        builder.HasKey(review => review.Id);

        // ValueGeneratedNever: Id is always app-assigned (StudyReview's constructor), never left
        // as Guid.Empty for EF to fill in. Without this, a review added only via StudyItem's
        // "_reviews" collection (never Add()-ed on a DbSet directly) is ambiguous to EF's
        // disconnected-entity heuristic — a non-default Guid key discovered through navigation
        // fixup reads as "probably already exists", so EF marks it Modified instead of Added and
        // issues an UPDATE that matches zero rows instead of the intended INSERT.
        builder.Property(review => review.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(review => review.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc")
            .IsRequired();

        builder.Property(review => review.ConfidenceRating)
            .HasColumnName("confidence_rating")
            .IsRequired();

        builder.Property(review => review.NotesMarkdown)
            .HasColumnName("notes_markdown");

        // Shadow FK — see StudyItemConfiguration's "_reviews" collection mapping.
        builder.Property<Guid>("StudyItemId")
            .HasColumnName("study_item_id");
    }
}
