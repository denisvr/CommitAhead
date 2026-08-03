using CommitAhead.Api.Tests.Auth;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CommitAhead.Api.Tests.StudyItems;

/// <summary>
/// Runs StudyItems endpoints against a real Testcontainers Postgres with migrations applied —
/// unlike AuthTestWebApplicationFactory's stubbed repository, these endpoints exercise the actual
/// EF mappings (JSONB details, ranked-list query), so a fake repository would prove nothing about
/// them. Uses the real EF-backed IUserRepository (not a stub): study_items/scoring_config_overrides
/// now have a real FK to users.id, so a StudyItem's owner must be a genuine row in this same
/// database, not just an in-memory stand-in. Still reuses AuthTestWebApplicationFactory's JWT
/// signing key/issuer constants — that part is unrelated to what this fixture verifies.
/// </summary>
public sealed class StudyItemsTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_api_test")
        .WithUsername("commitahead_api_test")
        .WithPassword("commitahead_api_test")
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
            });
        });

        builder.ConfigureServices(services =>
        {
            // See AuthTestWebApplicationFactory for why an ephemeral key ring is required here.
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
public sealed class StudyItemsApiCollection : ICollectionFixture<StudyItemsTestWebApplicationFactory>
{
    public const string Name = "StudyItemsApi";
}
