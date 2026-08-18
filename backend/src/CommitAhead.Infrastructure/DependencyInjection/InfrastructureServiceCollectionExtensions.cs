using CommitAhead.Application.Auth;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Infrastructure.Auth;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.ProfessionalProfiles;
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
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IProfessionalProfileRepository, ProfessionalProfileRepository>();
        services.AddScoped<ICVPresentationRepository, CVPresentationRepository>();

        // No .ValidateOnStart(): the build-time OpenAPI document generator actually runs the host
        // (not just builds it) without user-secrets loaded, so eager validation here would break
        // `dotnet build`. An unconfigured Supabase:Url/AnonKey instead fails lazily, the first
        // time ISupabaseAuthClient is actually used.
        services.AddOptions<SupabaseAuthOptions>()
            .Bind(configuration.GetSection(SupabaseAuthOptions.SectionName));

        // Same lazy-failure posture as SupabaseAuthOptions above, same reason: no .ValidateOnStart().
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName));

        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SupabaseAuthOptions>>().Value;
            ConfigureSupabaseBaseAddress(client, options);
        });

        services.AddScoped<IExportRenderer, QuestPdfCVExportRenderer>();

        return services;
    }

    /// <summary>
    /// An unconfigured/invalid Supabase:Url must not throw here — this delegate runs the moment
    /// SupabaseAuthClient is constructed via DI (e.g. on every /api/me-triggered token refresh),
    /// well before any use case's own try/catch around the actual HTTP call ever runs. Leaving
    /// BaseAddress unset instead means the client still constructs successfully; the first real
    /// request then fails with an ordinary, catchable InvalidOperationException from HttpClient
    /// itself, exactly like any other Supabase-call failure the surrounding use cases already handle.
    /// </summary>
    private static void ConfigureSupabaseBaseAddress(HttpClient client, SupabaseAuthOptions options)
    {
        if (Uri.TryCreate(options.Url, UriKind.Absolute, out var baseAddress))
        {
            client.BaseAddress = baseAddress;
        }

        client.DefaultRequestHeaders.Add("apikey", options.AnonKey ?? string.Empty);
    }
}
