using CommitAhead.Application.Auth;
using CommitAhead.Application.Identity;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginUseCase>();
        services.AddScoped<CallbackUseCase>();
        services.AddScoped<RefreshUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();

        services.AddScoped<CreateStudyItemUseCase>();
        services.AddScoped<UpdateStudyItemUseCase>();
        services.AddScoped<ArchiveStudyItemUseCase>();
        services.AddScoped<RestoreStudyItemUseCase>();
        services.AddScoped<DeleteStudyItemUseCase>();
        services.AddScoped<GetStudyItemUseCase>();
        services.AddScoped<GetStudyItemsUseCase>();
        services.AddScoped<SubmitStudyReviewUseCase>();
        services.AddScoped<SetPriorityOverrideUseCase>();
        services.AddScoped<ClearPriorityOverrideUseCase>();
        services.AddScoped<UpdateScoringConfigUseCase>();
        services.AddScoped<ResetScoringConfigUseCase>();
        services.AddScoped<GetScoringConfigUseCase>();
        services.AddScoped<GetRankedStudyQueueUseCase>();

        services.AddScoped<GetProfessionalProfileUseCase>();
        services.AddScoped<CreateProfessionalProfileUseCase>();
        services.AddScoped<UpdateProfessionalProfileUseCase>();

        // The seven ReplaceXUseCase classes (ReplaceExperienceUseCase, ReplaceEducationUseCase,
        // ReplaceSkillsUseCase, ReplaceLanguagesUseCase, ReplaceCertificationsUseCase,
        // ReplaceProjectsUseCase, ReplaceProfileLinksUseCase) are deliberately NOT registered here
        // right now: this slice gave them all an ICVPresentationRepository dependency (invariant-25
        // dangling-selection cleanup), and that interface has no Infrastructure implementation
        // until the CVPresentation Infrastructure slice. Registering them now would make the API
        // host fail to build (ValidateOnBuild), breaking every Api.Tests test. Re-add all seven
        // AddScoped<...>() lines, together with ICVPresentationRepository's own registration and
        // the CVPresentation use cases, once that slice lands.

        return services;
    }
}
