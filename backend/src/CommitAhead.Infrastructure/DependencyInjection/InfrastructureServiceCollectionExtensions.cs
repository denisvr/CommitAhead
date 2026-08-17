using CommitAhead.Application.AI;
using CommitAhead.Application.AIUsage;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Auth;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Application.Identity;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
using CommitAhead.Infrastructure.AI;
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
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommitAhead.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string environmentName)
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
        services.AddScoped<IEvidenceLinkRepository, EvidenceLinkRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

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

        // Reuses SupabaseAuthOptions (Url + AnonKey) rather than a redundant options type — Storage
        // and Auth are the same Supabase project's same two values (ADR-0018: no service-role key,
        // no new secret). The current request's user JWT is added per-call inside
        // SupabaseStorageClient itself, never as a default header here.
        services.AddHttpClient<IJobPostingStorage, SupabaseStorageClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SupabaseAuthOptions>>().Value;
            ConfigureSupabaseBaseAddress(client, options);
        });

        services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddScoped<IExportRenderer, QuestPdfCVExportRenderer>();

        AddAIProvider(services, configuration, environmentName);

        return services;
    }

    /// <summary>
    /// An unconfigured/invalid Supabase:Url must not throw here — this delegate runs the moment
    /// SupabaseAuthClient/SupabaseStorageClient is constructed via DI (e.g. on every /api/me-
    /// triggered token refresh), well before any use case's own try/catch around the actual HTTP
    /// call ever runs. Leaving BaseAddress unset instead means the client still constructs
    /// successfully; the first real request then fails with an ordinary, catchable
    /// InvalidOperationException from HttpClient itself, exactly like any other Supabase-call
    /// failure the surrounding use cases already handle.
    /// </summary>
    private static void ConfigureSupabaseBaseAddress(HttpClient client, SupabaseAuthOptions options)
    {
        if (Uri.TryCreate(options.Url, UriKind.Absolute, out var baseAddress))
        {
            client.BaseAddress = baseAddress;
        }

        client.DefaultRequestHeaders.Add("apikey", options.AnonKey ?? string.Empty);
    }

    /// <summary>
    /// Explicit, configuration-driven provider selection (ADR-0019) — one switch, evaluated once
    /// at composition-root time, never a plugin/discovery mechanism, runtime fallback, or per-user
    /// routing. "AI:Provider" is not secret (just a provider name), so it has a checked-in
    /// appsettings.json default — the build-time OpenAPI generator runs this host without
    /// user-secrets loaded and still needs a value to resolve against. Adding a second provider
    /// later means one new case here, its own options/HTTP registration, and its own tests — no
    /// change to Domain, the AnalyzeX use cases, AnalysisCommandOrchestrator, or any controller.
    /// </summary>
    private static void AddAIProvider(IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        const string AnthropicClientName = "AnthropicAIProvider";

        switch (configuration["AI:Provider"])
        {
            case "Anthropic":
                services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.SectionName));

                // HttpClient's own built-in logging only ever emits header values at Trace, and
                // this app's appsettings.json never configures anything below Information — this
                // filter makes that hold even if a future config change lowers the global default.
                services.AddLogging(logging => logging.AddFilter($"System.Net.Http.HttpClient.{AnthropicClientName}", LogLevel.Warning));

                // Belt-and-suspenders on top of the filter above: IHttpClientFactory's own logging
                // handlers redact any header this predicate matches before they ever format a log
                // message, regardless of the configured level.
                services.Configure<HttpClientFactoryOptions>(AnthropicClientName, options =>
                    options.ShouldRedactHeaderValue = header => string.Equals(header, "x-api-key", StringComparison.OrdinalIgnoreCase));

                // Deliberately does not read AnthropicOptions.ApiKey here: this delegate runs
                // whenever IHttpClientFactory builds the named client, which happens whenever
                // IAIProvider is resolved — and every AnalyzeX use case constructor-injects
                // IAIProvider, so every request to JobAnalyses/CVPresentations/InterviewNotes
                // (GET/PUT/DELETE included, not just analyze) would otherwise require the API key
                // to be configured just to construct the controller. AnthropicAIProvider reads and
                // validates the key lazily, only when a provider method is actually invoked.
                //
                // BaseAddress IS resolved eagerly here (unlike the API key) because it is not a
                // secret and AnthropicBaseAddress.Resolve is the fail-closed guard against a
                // misconfigured/untrusted host (E2E Foundation Plan) — it must run whenever the
                // named client is built, not be deferrable to first real use.
                services.AddHttpClient(AnthropicClientName, (serviceProvider, client) =>
                    {
                        var options = serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                        client.BaseAddress = AnthropicBaseAddress.Resolve(options.BaseUrl, environmentName);
                        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    })
                    .AddTypedClient<IAIProvider>((httpClient, serviceProvider) =>
                        new AnthropicAIProvider(httpClient, serviceProvider.GetRequiredService<IOptions<AnthropicOptions>>()));
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown or unconfigured AI:Provider value: '{configuration["AI:Provider"] ?? "(none)"}'. Supported: 'Anthropic'.");
        }
    }
}
