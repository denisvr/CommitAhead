using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Api.Features.ProfessionalProfiles;

/// <summary>
/// The wire-contract counterparts of ProfessionalProfile's value objects and child entities. Kept
/// separate from Infrastructure's own persistence shapes — Api must not depend on Infrastructure.
/// Not "*Controller" types, so they may reference Domain types freely (NetArchTest rule 4 only
/// restricts controllers themselves); all Domain-touching for ProfessionalProfile lives here, never
/// in a controller body. Every entry's Id is client-supplied (a fresh Guid for a new entry, the
/// existing one when editing) — Replace* endpoints take the caller's complete desired collection,
/// and preserving Id across an edit is what keeps a CVPresentation's selections from going stale.
/// </summary>
public sealed record ContactInfoDto(string Name, string Email, string? Phone, string? Address, string? PhotoStorageKey)
{
    public ContactInfo ToDomain() => new(Name, Email, Phone, Address, PhotoStorageKey);

    public static ContactInfoDto FromDomain(ContactInfo contactInfo) => new(contactInfo.Name, contactInfo.Email, contactInfo.Phone, contactInfo.Address, contactInfo.PhotoStorageKey);
}

public sealed record YearMonthDto(int Year, int Month)
{
    public YearMonth ToDomain() => new(Year, Month);

    public static YearMonthDto FromDomain(YearMonth yearMonth) => new(yearMonth.Year, yearMonth.Month);
}

public sealed record ExperienceEntryDto(
    Guid Id,
    string Company,
    string? Client,
    string Role,
    EmploymentType EmploymentType,
    YearMonthDto StartDate,
    YearMonthDto? EndDate,
    string? Location,
    WorkMode WorkMode,
    string SummaryMarkdown,
    IReadOnlyList<string> Achievements,
    IReadOnlyList<Guid> SkillIds)
{
    public ExperienceEntry ToDomain() => new(
        Id, Company, Client, Role, EmploymentType, StartDate.ToDomain(), EndDate?.ToDomain(), Location, WorkMode, SummaryMarkdown, Achievements, SkillIds);

    public static ExperienceEntryDto FromDomain(ExperienceEntry entry) => new(
        entry.Id, entry.Company, entry.Client, entry.Role, entry.EmploymentType,
        YearMonthDto.FromDomain(entry.StartDate), entry.EndDate is null ? null : YearMonthDto.FromDomain(entry.EndDate),
        entry.Location, entry.WorkMode, entry.SummaryMarkdown, entry.Achievements, entry.SkillIds);
}

public sealed record EducationEntryDto(
    Guid Id, string Institution, string Degree, string? Field, YearMonthDto? StartDate, YearMonthDto? EndDate, string? Location, string? DetailsMarkdown)
{
    public EducationEntry ToDomain() => new(Id, Institution, Degree, Field, StartDate?.ToDomain(), EndDate?.ToDomain(), Location, DetailsMarkdown);

    public static EducationEntryDto FromDomain(EducationEntry entry) => new(
        entry.Id, entry.Institution, entry.Degree, entry.Field,
        entry.StartDate is null ? null : YearMonthDto.FromDomain(entry.StartDate), entry.EndDate is null ? null : YearMonthDto.FromDomain(entry.EndDate),
        entry.Location, entry.DetailsMarkdown);
}

/// <summary>NormalizedKey is read-only — present so a response can display it, ignored by ToDomain() since Skill's constructor always recomputes it from DisplayName.</summary>
public sealed record SkillDto(Guid Id, string DisplayName, string NormalizedKey, SkillCategory Category, SkillProficiency? Proficiency)
{
    public Skill ToDomain() => new(Id, DisplayName, Category, Proficiency);

    public static SkillDto FromDomain(Skill skill) => new(skill.Id, skill.DisplayName, skill.NormalizedKey, skill.Category, skill.Proficiency);
}

public sealed record LanguageEntryDto(Guid Id, string Language, LanguageProficiency Proficiency, string? Certification)
{
    public LanguageEntry ToDomain() => new(Id, Language, Proficiency, Certification);

    public static LanguageEntryDto FromDomain(LanguageEntry entry) => new(entry.Id, entry.Language, entry.Proficiency, entry.Certification);
}

public sealed record CertificationEntryDto(
    Guid Id, string Name, string IssuingOrganisation, YearMonthDto? IssuedAt, YearMonthDto? ExpiresAt, string? CredentialId, string? Url)
{
    public CertificationEntry ToDomain() => new(Id, Name, IssuingOrganisation, IssuedAt?.ToDomain(), ExpiresAt?.ToDomain(), CredentialId, Url);

    public static CertificationEntryDto FromDomain(CertificationEntry entry) => new(
        entry.Id, entry.Name, entry.IssuingOrganisation,
        entry.IssuedAt is null ? null : YearMonthDto.FromDomain(entry.IssuedAt), entry.ExpiresAt is null ? null : YearMonthDto.FromDomain(entry.ExpiresAt),
        entry.CredentialId, entry.Url);
}

public sealed record ProjectEntryDto(
    Guid Id, string Name, string? Role, YearMonthDto? StartDate, YearMonthDto? EndDate, string DescriptionMarkdown, string? Url, IReadOnlyList<Guid> SkillIds)
{
    public ProjectEntry ToDomain() => new(Id, Name, Role, StartDate?.ToDomain(), EndDate?.ToDomain(), DescriptionMarkdown, Url, SkillIds);

    public static ProjectEntryDto FromDomain(ProjectEntry entry) => new(
        entry.Id, entry.Name, entry.Role,
        entry.StartDate is null ? null : YearMonthDto.FromDomain(entry.StartDate), entry.EndDate is null ? null : YearMonthDto.FromDomain(entry.EndDate),
        entry.DescriptionMarkdown, entry.Url, entry.SkillIds);
}

public sealed record ProfileLinkDto(Guid Id, ProfileLinkKind Kind, string? Label, string Url)
{
    public ProfileLink ToDomain() => new(Id, Kind, Label, Url);

    public static ProfileLinkDto FromDomain(ProfileLink link) => new(link.Id, link.Kind, link.Label, link.Url);
}

/// <summary>
/// Domain-touching Replace* calls live here, not inline in the controller — a controller method
/// that itself calls ToDomain() would embed a Domain type reference in its own IL and trip
/// ArchitectureTests.Controllers_ShouldOnlyDependOnApplication_NotInfrastructureOrDomain.
/// </summary>
public static class ProfessionalProfileCollectionRequestExtensions
{
    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<ExperienceEntryDto> experience, ReplaceExperienceUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(experience.Select(entry => entry.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<EducationEntryDto> education, ReplaceEducationUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(education.Select(entry => entry.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<SkillDto> skills, ReplaceSkillsUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(skills.Select(skill => skill.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<LanguageEntryDto> languages, ReplaceLanguagesUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(languages.Select(entry => entry.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<CertificationEntryDto> certifications, ReplaceCertificationsUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(certifications.Select(entry => entry.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<ProjectEntryDto> projects, ReplaceProjectsUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(projects.Select(entry => entry.ToDomain()).ToList(), cancellationToken);

    public static Task<ProfessionalProfileMutationResult> ReplaceAsync(this IReadOnlyList<ProfileLinkDto> profileLinks, ReplaceProfileLinksUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(profileLinks.Select(link => link.ToDomain()).ToList(), cancellationToken);
}
