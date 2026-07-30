using CommitAhead.Application.Auth;
using CommitAhead.Application.Identity;
using CommitAhead.Application.StudyItems;
using CommitAhead.Infrastructure.Auth;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
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

        return services;
    }
}
