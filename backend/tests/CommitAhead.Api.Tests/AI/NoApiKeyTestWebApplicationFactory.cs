using CommitAhead.Api.Tests.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using CommitAhead.Infrastructure.Persistence;

namespace CommitAhead.Api.Tests.AI;

/// <summary>
/// Deliberately does NOT override IAIProvider with a fake — the whole point is to exercise the
/// real AddInfrastructure/AnthropicAIProvider DI registration path with no
/// AI:Providers:Anthropic:ApiKey configured, and prove that ordinary, non-AI endpoints on
/// JobAnalyses/CVPresentations/InterviewNotes still work. A real Anthropic call is never made by
/// any test using this factory (ADR-0009) — only GET/PUT/DELETE actions are exercised, and the
/// `analyze` actions are never invoked here.
/// </summary>
public sealed class NoApiKeyTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_no_api_key_test")
        .WithUsername("commitahead_no_api_key_test")
        .WithPassword("commitahead_no_api_key_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_container.GetConnectionString()).Options;
        using var dbContext = new CommitAheadDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CommitAheadDb"] = _container.GetConnectionString(),
                ["AI:Providers:Anthropic:ApiKey"] = null,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuer = AuthTestWebApplicationFactory.TestIssuer;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = "authenticated";
                options.TokenValidationParameters.IssuerSigningKey = AuthTestWebApplicationFactory.SigningKey;
            });
        });
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NoApiKeyApiCollection : ICollectionFixture<NoApiKeyTestWebApplicationFactory>
{
    public const string Name = "NoApiKeyApi";
}
