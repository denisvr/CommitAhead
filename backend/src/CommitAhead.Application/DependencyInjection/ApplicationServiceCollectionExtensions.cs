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
        services.AddScoped<ReplaceExperienceUseCase>();
        services.AddScoped<ReplaceEducationUseCase>();
        services.AddScoped<ReplaceSkillsUseCase>();
        services.AddScoped<ReplaceLanguagesUseCase>();
        services.AddScoped<ReplaceCertificationsUseCase>();
        services.AddScoped<ReplaceProjectsUseCase>();
        services.AddScoped<ReplaceProfileLinksUseCase>();

        return services;
    }
}
