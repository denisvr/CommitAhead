using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Auth;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.Auth;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.InterviewNotes;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
using CommitAhead.Infrastructure.StudyItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommitAhead.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CommitAheadDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CommitAheadDb")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudyItemRepository, StudyItemRepository>();
        services.AddScoped<IScoringConfigRepository, ScoringConfigRepository>();
        services.AddScoped<IRankedStudyQueueQuery, RankedStudyQueueQuery>();
        services.AddScoped<IEvidenceLinkQuery, EvidenceLinkQuery>();
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IProfessionalProfileRepository, ProfessionalProfileRepository>();
        services.AddScoped<ICVPresentationRepository, CVPresentationRepository>();
        services.AddScoped<IJobAnalysisRepository, JobAnalysisRepository>();
        services.AddScoped<IInterviewNoteRepository, InterviewNoteRepository>();
        services.AddScoped<IAnalysisDraftRepository, AnalysisDraftRepository>();
        services.AddScoped<IAIUsageRecordRepository, AIUsageRecordRepository>();

        // No .ValidateOnStart(): the build-time OpenAPI document generator actually runs the host
        // (not just builds it) without user-secrets loaded, so eager validation here would break
        // `dotnet build`. An unconfigured Supabase:Url/AnonKey instead fails lazily, the first
        // time ISupabaseAuthClient is actually used.
        services.AddOptions<SupabaseAuthOptions>()
            .Bind(configuration.GetSection(SupabaseAuthOptions.SectionName));

        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SupabaseAuthOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
            client.DefaultRequestHeaders.Add("apikey", options.AnonKey);
        });

        // Reuses SupabaseAuthOptions (Url + AnonKey) rather than a redundant options type — Storage
        // and Auth are the same Supabase project's same two values (ADR-0018: no service-role key,
        // no new secret). The current request's user JWT is added per-call inside
        // SupabaseStorageClient itself, never as a default header here.
        services.AddHttpClient<IJobPostingStorage, SupabaseStorageClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SupabaseAuthOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
            client.DefaultRequestHeaders.Add("apikey", options.AnonKey);
        });

        services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();

        return services;
    }
}
