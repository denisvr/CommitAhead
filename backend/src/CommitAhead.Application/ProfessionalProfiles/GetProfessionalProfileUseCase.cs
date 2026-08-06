using CommitAhead.Application.Identity;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public sealed class GetProfessionalProfileUseCase
{
    private readonly IProfessionalProfileRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetProfessionalProfileUseCase(IProfessionalProfileRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>Null before the current user's first save — not an error, ProfessionalProfile has no default state to fall back to.</summary>
    public async Task<ProfessionalProfileResult?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByOwnerUserIdAsync(_currentUser.UserId, cancellationToken);
        return profile is null ? null : ProfessionalProfileResult.FromDomain(profile);
    }
}

public sealed record ProfessionalProfileResult(
    Guid Id,
    ContactInfo ContactInfo,
    string SummaryMarkdown,
    IReadOnlyList<ExperienceEntry> Experience,
    IReadOnlyList<EducationEntry> Education,
    IReadOnlyList<Skill> Skills,
    IReadOnlyList<LanguageEntry> Languages,
    IReadOnlyList<CertificationEntry> Certifications,
    IReadOnlyList<ProjectEntry> Projects,
    IReadOnlyList<ProfileLink> ProfileLinks,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static ProfessionalProfileResult FromDomain(ProfessionalProfile profile) => new(
        profile.Id,
        profile.ContactInfo,
        profile.SummaryMarkdown,
        profile.Experience,
        profile.Education,
        profile.Skills,
        profile.Languages,
        profile.Certifications,
        profile.Projects,
        profile.ProfileLinks,
        profile.CreatedAtUtc,
        profile.UpdatedAtUtc);
}
