using CommitAhead.Application.Auth;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.JobAnalyses;
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

        services.AddScoped<GetCVPresentationUseCase>();
        services.AddScoped<GetCVPresentationsUseCase>();
        services.AddScoped<CreateCVPresentationUseCase>();
        services.AddScoped<UpdateCVPresentationUseCase>();
        services.AddScoped<DeleteCVPresentationUseCase>();
        services.AddScoped<ReplaceExperienceSelectionsUseCase>();
        services.AddScoped<ReplaceEducationSelectionsUseCase>();
        services.AddScoped<ReplaceSkillSelectionsUseCase>();
        services.AddScoped<ReplaceLanguageSelectionsUseCase>();
        services.AddScoped<ReplaceCertificationSelectionsUseCase>();
        services.AddScoped<ReplaceProjectSelectionsUseCase>();
        services.AddScoped<ReplaceProfileLinkSelectionsUseCase>();

        services.AddScoped<CreateJobAnalysisUseCase>();
        services.AddScoped<CreateJobAnalysisFromUploadUseCase>();
        services.AddScoped<UpdateJobAnalysisUseCase>();
        services.AddScoped<DeleteJobAnalysisUseCase>();
        services.AddScoped<GetJobAnalysisUseCase>();
        services.AddScoped<GetJobAnalysesUseCase>();

        services.AddScoped<CreateInterviewNoteUseCase>();
        services.AddScoped<UpdateInterviewNoteUseCase>();
        services.AddScoped<DeleteInterviewNoteUseCase>();
        services.AddScoped<GetInterviewNoteUseCase>();
        services.AddScoped<GetInterviewNotesUseCase>();

        return services;
    }
}
