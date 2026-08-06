using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommitAhead.Infrastructure.CVPresentations;

public sealed class CVPresentationConfiguration : IEntityTypeConfiguration<CVPresentation>
{
    public void Configure(EntityTypeBuilder<CVPresentation> builder)
    {
        builder.ToTable("cv_presentations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        // Real FK, not just a plain UUID column (model.md: CVPresentation references
        // ProfessionalProfile). Restrict: this app has no ProfessionalProfile-deletion use case at
        // all (ADR-0012), matching every other user-owned aggregate's OwnerUserId FK.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.ProfessionalProfileId)
            .HasColumnName("professional_profile_id")
            .IsRequired();

        builder.HasOne<ProfessionalProfile>()
            .WithMany()
            .HasForeignKey(p => p.ProfessionalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Label).HasColumnName("label").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(p => p.TargetMarket).HasColumnName("target_market").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(p => p.TargetRole).HasColumnName("target_role").HasMaxLength(ValidationLimits.ShortTextMaxLength);
        builder.Property(p => p.Locale).HasColumnName("locale").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(p => p.TemplateKey).HasColumnName("template_key").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(p => p.SummaryOverrideMarkdown).HasColumnName("summary_override_markdown").HasMaxLength(ValidationLimits.MarkdownMaxLength);
        builder.Property(p => p.IncludePhoto).HasColumnName("include_photo").IsRequired();
        builder.Property(p => p.IncludeEmail).HasColumnName("include_email").IsRequired();
        builder.Property(p => p.IncludePhone).HasColumnName("include_phone").IsRequired();
        builder.Property(p => p.IncludeAddress).HasColumnName("include_address").IsRequired();
        builder.Property(p => p.DateFormat).HasColumnName("date_format").HasMaxLength(ValidationLimits.ShortTextMaxLength).IsRequired();
        builder.Property(p => p.PageLimit).HasColumnName("page_limit").IsRequired();
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        // uuid[] per selection, same technique as StudyItem.Tags and ProfessionalProfile's
        // ExperienceEntry/ProjectEntry.SkillIds — list order IS position (CVPresentation's own
        // comment), so there is no separate position column; no FK to the canonical entry tables
        // for the same EF constructor-binding reason documented on ExperienceEntryConfiguration.
        builder.Property(p => p.ExperienceSelections).HasColumnName("experience_selections").IsRequired();
        builder.Property(p => p.EducationSelections).HasColumnName("education_selections").IsRequired();
        builder.Property(p => p.SkillSelections).HasColumnName("skill_selections").IsRequired();
        builder.Property(p => p.LanguageSelections).HasColumnName("language_selections").IsRequired();
        builder.Property(p => p.CertificationSelections).HasColumnName("certification_selections").IsRequired();
        builder.Property(p => p.ProjectSelections).HasColumnName("project_selections").IsRequired();
        builder.Property(p => p.ProfileLinkSelections).HasColumnName("profile_link_selections").IsRequired();

        builder.HasIndex(p => p.OwnerUserId);
    }
}
