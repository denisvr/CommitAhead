using CommitAhead.Api.Security;
using CommitAhead.Application.ProfessionalProfiles;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.ProfessionalProfiles;

/// <summary>A singleton per owner (model.md) — no {id} anywhere in this route; the current user always has at most one.</summary>
[ApiController]
[Route("api/professional-profile")]
[UsesOwnerScopedData]
public sealed class ProfessionalProfileController : ControllerBase
{
    private readonly GetProfessionalProfileUseCase _getUseCase;
    private readonly CreateProfessionalProfileUseCase _createUseCase;
    private readonly UpdateProfessionalProfileUseCase _updateUseCase;
    private readonly ReplaceExperienceUseCase _replaceExperienceUseCase;
    private readonly ReplaceEducationUseCase _replaceEducationUseCase;
    private readonly ReplaceSkillsUseCase _replaceSkillsUseCase;
    private readonly ReplaceLanguagesUseCase _replaceLanguagesUseCase;
    private readonly ReplaceCertificationsUseCase _replaceCertificationsUseCase;
    private readonly ReplaceProjectsUseCase _replaceProjectsUseCase;
    private readonly ReplaceProfileLinksUseCase _replaceProfileLinksUseCase;

    public ProfessionalProfileController(
        GetProfessionalProfileUseCase getUseCase,
        CreateProfessionalProfileUseCase createUseCase,
        UpdateProfessionalProfileUseCase updateUseCase,
        ReplaceExperienceUseCase replaceExperienceUseCase,
        ReplaceEducationUseCase replaceEducationUseCase,
        ReplaceSkillsUseCase replaceSkillsUseCase,
        ReplaceLanguagesUseCase replaceLanguagesUseCase,
        ReplaceCertificationsUseCase replaceCertificationsUseCase,
        ReplaceProjectsUseCase replaceProjectsUseCase,
        ReplaceProfileLinksUseCase replaceProfileLinksUseCase)
    {
        _getUseCase = getUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _replaceExperienceUseCase = replaceExperienceUseCase;
        _replaceEducationUseCase = replaceEducationUseCase;
        _replaceSkillsUseCase = replaceSkillsUseCase;
        _replaceLanguagesUseCase = replaceLanguagesUseCase;
        _replaceCertificationsUseCase = replaceCertificationsUseCase;
        _replaceProjectsUseCase = replaceProjectsUseCase;
        _replaceProfileLinksUseCase = replaceProfileLinksUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ProfessionalProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(cancellationToken);
        return result is null ? NotFound() : Ok(ProfessionalProfileResponse.FromResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<ProfessionalProfileCreatedResponse>> Post([FromBody] CreateProfessionalProfileRequest request, CancellationToken cancellationToken)
    {
        var id = await request.CreateAsync(_createUseCase, cancellationToken);
        return id is null ? Conflict() : CreatedAtAction(nameof(Get), null, new ProfessionalProfileCreatedResponse(id.Value));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UpdateProfessionalProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await request.UpdateAsync(_updateUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("experience")]
    public async Task<IActionResult> PutExperience([FromBody] IReadOnlyList<ExperienceEntryDto> experience, CancellationToken cancellationToken)
    {
        var result = await experience.ReplaceAsync(_replaceExperienceUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("education")]
    public async Task<IActionResult> PutEducation([FromBody] IReadOnlyList<EducationEntryDto> education, CancellationToken cancellationToken)
    {
        var result = await education.ReplaceAsync(_replaceEducationUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("skills")]
    public async Task<IActionResult> PutSkills([FromBody] IReadOnlyList<SkillDto> skills, CancellationToken cancellationToken)
    {
        var result = await skills.ReplaceAsync(_replaceSkillsUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("languages")]
    public async Task<IActionResult> PutLanguages([FromBody] IReadOnlyList<LanguageEntryDto> languages, CancellationToken cancellationToken)
    {
        var result = await languages.ReplaceAsync(_replaceLanguagesUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("certifications")]
    public async Task<IActionResult> PutCertifications([FromBody] IReadOnlyList<CertificationEntryDto> certifications, CancellationToken cancellationToken)
    {
        var result = await certifications.ReplaceAsync(_replaceCertificationsUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("projects")]
    public async Task<IActionResult> PutProjects([FromBody] IReadOnlyList<ProjectEntryDto> projects, CancellationToken cancellationToken)
    {
        var result = await projects.ReplaceAsync(_replaceProjectsUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("profile-links")]
    public async Task<IActionResult> PutProfileLinks([FromBody] IReadOnlyList<ProfileLinkDto> profileLinks, CancellationToken cancellationToken)
    {
        var result = await profileLinks.ReplaceAsync(_replaceProfileLinksUseCase, cancellationToken);
        return result == ProfessionalProfileMutationResult.NotFound ? NotFound() : NoContent();
    }
}

public sealed record ProfessionalProfileCreatedResponse(Guid Id);

public sealed record CreateProfessionalProfileRequest(ContactInfoDto ContactInfo, string SummaryMarkdown)
{
    public Task<Guid?> CreateAsync(CreateProfessionalProfileUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(ContactInfo.ToDomain(), SummaryMarkdown, cancellationToken);
}

public sealed record UpdateProfessionalProfileRequest(ContactInfoDto ContactInfo, string SummaryMarkdown)
{
    public Task<ProfessionalProfileMutationResult> UpdateAsync(UpdateProfessionalProfileUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(ContactInfo.ToDomain(), SummaryMarkdown, cancellationToken);
}

public sealed record ProfessionalProfileResponse(
    Guid Id,
    ContactInfoDto ContactInfo,
    string SummaryMarkdown,
    IReadOnlyList<ExperienceEntryDto> Experience,
    IReadOnlyList<EducationEntryDto> Education,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<LanguageEntryDto> Languages,
    IReadOnlyList<CertificationEntryDto> Certifications,
    IReadOnlyList<ProjectEntryDto> Projects,
    IReadOnlyList<ProfileLinkDto> ProfileLinks,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static ProfessionalProfileResponse FromResult(ProfessionalProfileResult result) => new(
        result.Id,
        ContactInfoDto.FromDomain(result.ContactInfo),
        result.SummaryMarkdown,
        result.Experience.Select(ExperienceEntryDto.FromDomain).ToList(),
        result.Education.Select(EducationEntryDto.FromDomain).ToList(),
        result.Skills.Select(SkillDto.FromDomain).ToList(),
        result.Languages.Select(LanguageEntryDto.FromDomain).ToList(),
        result.Certifications.Select(CertificationEntryDto.FromDomain).ToList(),
        result.Projects.Select(ProjectEntryDto.FromDomain).ToList(),
        result.ProfileLinks.Select(ProfileLinkDto.FromDomain).ToList(),
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}
