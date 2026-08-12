using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.CVPresentations;

public class ExportCVPresentationUseCaseTests
{
    private static ExportCVPresentationUseCase CreateUseCase(
        FakeCVPresentationRepository presentationRepository, FakeProfessionalProfileRepository profileRepository, FakeExportRenderer renderer, Guid ownerUserId) =>
        new(presentationRepository, profileRepository, renderer, new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    private static ProfessionalProfile CreateProfile(Guid ownerUserId, out Guid experienceId, out Guid educationId, out Guid skillId)
    {
        var profile = new ProfessionalProfile(
            Guid.NewGuid(), ownerUserId, new ContactInfo("Ada Lovelace", "ada@example.com", "+1 555 0100", "123 Analytical Engine St", null), "A summary.", DateTime.UtcNow);

        var experience = new ExperienceEntry(
            Guid.NewGuid(), "Acme Corp", null, "Senior Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), new YearMonth(2023, 6), "Remote", WorkMode.Remote,
            "Led backend systems.", ["Shipped the payments platform."], []);
        var education = new EducationEntry(Guid.NewGuid(), "State University", "BSc Computer Science", null, new YearMonth(2016, 9), new YearMonth(2020, 6), null, null);
        var skill = new Skill(Guid.NewGuid(), "C#", SkillCategory.Language, SkillProficiency.Expert);

        profile.ReplaceExperience([experience], DateTime.UtcNow);
        profile.ReplaceEducation([education], DateTime.UtcNow);
        profile.ReplaceSkills([skill], DateTime.UtcNow);

        experienceId = experience.Id;
        educationId = education.Id;
        skillId = skill.Id;
        return profile;
    }

    private static CVPresentation CreatePresentation(
        Guid ownerUserId, Guid professionalProfileId, IEnumerable<Guid> experienceIds, IEnumerable<Guid> skillIds, int pageLimit = 5,
        bool includeEmail = true, bool includePhone = true, bool includeAddress = true,
        string templateKey = ExportCVPresentationUseCase.SupportedTemplateKey, bool includePhoto = false)
    {
        var presentation = new CVPresentation(
            Guid.NewGuid(), ownerUserId, professionalProfileId, "US Resume", "United States", "Backend Engineer", "en-US", templateKey,
            null, includePhoto, includeEmail, includePhone, includeAddress, "MMM yyyy", pageLimit, DateTime.UtcNow);

        presentation.ReplaceExperienceSelections(experienceIds, DateTime.UtcNow);
        presentation.ReplaceSkillSelections(skillIds, DateTime.UtcNow);
        return presentation;
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSuchPresentation_ReturnsPresentationNotFound()
    {
        var useCase = CreateUseCase(new FakeCVPresentationRepository(), new FakeProfessionalProfileRepository(), new FakeExportRenderer(), Guid.NewGuid());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.PresentationNotFound, result.Outcome);
        Assert.Null(result.PdfBytes);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheOwnersProfileIsMissing_Throws()
    {
        var ownerUserId = Guid.NewGuid();
        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, Guid.NewGuid(), [], []);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var useCase = CreateUseCase(presentationRepository, new FakeProfessionalProfileRepository(), new FakeExportRenderer(), ownerUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(presentation.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ForAnotherOwnersPresentation_ReturnsPresentationNotFound()
    {
        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(Guid.NewGuid(), Guid.NewGuid(), [], []);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var useCase = CreateUseCase(presentationRepository, new FakeProfessionalProfileRepository(), new FakeExportRenderer(), Guid.NewGuid());

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.PresentationNotFound, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_ForAValidPresentation_ResolvesSelectionsAndAppliesVisibilityFlags()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId], [skillId], includeEmail: true, includePhone: false, includeAddress: false);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer();
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.Exported, result.Outcome);
        Assert.NotNull(renderer.LastDocument);
        var document = renderer.LastDocument!;

        Assert.Equal("Ada Lovelace", document.Contact.Name);
        Assert.Equal("ada@example.com", document.Contact.Email);
        Assert.Null(document.Contact.Phone);
        Assert.Null(document.Contact.Address);

        var experience = Assert.Single(document.Experience);
        Assert.Equal("Acme Corp", experience.Company);
        Assert.Equal("Senior Engineer", experience.Role);

        Assert.Equal("C#", Assert.Single(document.Skills));
    }

    [Fact]
    public async Task ExecuteAsync_SkipsADanglingSelectionForAnEntryThatNoLongerExists()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var danglingId = Guid.NewGuid();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId, danglingId], [skillId]);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer();
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Single(renderer.LastDocument!.Experience);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheRenderedPdfFitsWithinThePageLimit_ReturnsExportedWithTheBytes()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId], [skillId], pageLimit: 2);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer { PagesToGenerate = 1 };
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.Exported, result.Outcome);
        Assert.NotNull(result.PdfBytes);
        Assert.Equal(1, result.PageCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheRenderedPdfExceedsThePageLimit_ReturnsPageLimitExceededWithNoBytes()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId], [skillId], pageLimit: 1);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer { PagesToGenerate = 2 };
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.PageLimitExceeded, result.Outcome);
        Assert.Null(result.PdfBytes);
        Assert.Equal(2, result.PageCount);
    }

    [Fact]
    public async Task ExecuteAsync_ForAPresentationWithAnUnsupportedTemplateKey_ReturnsUnsupportedTemplateWithoutRendering()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId], [skillId], templateKey: "some-other-template");
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer();
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.UnsupportedTemplate, result.Outcome);
        Assert.Null(result.PdfBytes);
        Assert.Null(renderer.LastDocument);
    }

    [Fact]
    public async Task ExecuteAsync_ForAPresentationWithIncludePhotoTrue_ReturnsUnsupportedPhotoWithoutRendering()
    {
        var ownerUserId = Guid.NewGuid();
        var profile = CreateProfile(ownerUserId, out var experienceId, out _, out var skillId);
        var profileRepository = new FakeProfessionalProfileRepository();
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var presentationRepository = new FakeCVPresentationRepository();
        var presentation = CreatePresentation(ownerUserId, profile.Id, [experienceId], [skillId], includePhoto: true);
        await presentationRepository.AddAsync(presentation, CancellationToken.None);

        var renderer = new FakeExportRenderer();
        var useCase = CreateUseCase(presentationRepository, profileRepository, renderer, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, CancellationToken.None);

        Assert.Equal(ExportCVPresentationOutcome.UnsupportedPhoto, result.Outcome);
        Assert.Null(result.PdfBytes);
        Assert.Null(renderer.LastDocument);
    }

    private sealed class FakeExportRenderer : IExportRenderer
    {
        public int PagesToGenerate { get; set; } = 1;
        public CVExportDocument? LastDocument { get; private set; }

        public RenderedCVExport Render(CVExportDocument document)
        {
            LastDocument = document;
            return new RenderedCVExport([1, 2, 3], PagesToGenerate);
        }
    }
}
