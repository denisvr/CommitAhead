using System.Net;
using System.Net.Http.Json;
using CommitAhead.Api.Features.StudyItems;
using CommitAhead.Api.Tests.Auth;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Domain.Identity;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// The one focused HTTP-level proof that Phase 1 RLS actually protects a real request end to
/// end — real JWT auth, RlsTransactionActionFilter, and a genuine commitahead_app connection with
/// RLS enabled — as opposed to every other StudyItems API test (StudyItemsTestWebApplicationFactory),
/// which deliberately connects as the Testcontainers-provisioned owner role so it can test business
/// logic in isolation from RLS. Not a broad E2E suite: two tests, mirroring exactly what item 5 of
/// this corrective pass asks for. Bootstraps the same way setup-local-db.ps1 bootstraps a real
/// database: 001_roles.sql (superuser) -> EF migrations (commitahead_migrator) -> 002/003 RLS
/// scripts (superuser) -> the running API itself connects as commitahead_app.
/// </summary>
public sealed class RlsHttpIsolationTests : IAsyncLifetime
{
    private const string MigratorPassword = "rls-http-migrator-password";
    private const string AppPassword = "rls-http-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("commitahead_rls_http_test")
        .WithUsername("postgres")
        .WithPassword("rls-http-superuser-password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    private string SuperuserConnectionString => _container.GetConnectionString();

    private string MigratorConnectionString => WithCredentials("commitahead_migrator", MigratorPassword);

    private string AppConnectionString => WithCredentials("commitahead_app", AppPassword);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ExecuteAsSuperuserAsync(
            ReadScript("001_roles.sql")
                .Replace("${COMMITAHEAD_MIGRATOR_PASSWORD}", MigratorPassword)
                .Replace("${COMMITAHEAD_APP_PASSWORD}", AppPassword));

        var migratorOptions = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(MigratorConnectionString).Options;
        await using var migratorDbContext = new CommitAheadDbContext(migratorOptions);
        await migratorDbContext.Database.MigrateAsync();

        await ExecuteAsSuperuserAsync(ReadScript("002_rls_users.sql") + "\n" + ReadScript("003_rls_phase1.sql"));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // The running API connects as commitahead_app for this entire fixture — not the
            // superuser/migrator role every other StudyItems API test uses — so every request
            // really goes through RLS, not just the app's own owner_user_id filtering.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CommitAheadDb"] = AppConnectionString,
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
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private string WithCredentials(string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Username = username, Password = password };
        return builder.ToString();
    }

    private async Task ExecuteAsSuperuserAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(SuperuserConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string ReadScript(string fileName)
    {
        // Tests run from backend/tests/CommitAhead.Api.Tests/bin/<Config>/<TFM>/.
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "database", fileName));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not locate {fileName} at {path}. Expected backend/scripts/database/ relative to the test output directory.");
        }

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Provisioning is privileged and outside the application (item 2 of this corrective pass) —
    /// commitahead_app only has SELECT on `users`, so this inserts via the superuser connection,
    /// the way a real admin-driven invite would, never through the app-role connection the
    /// running API itself uses.
    /// </summary>
    private async Task<string> CreateAuthenticatedUserCookieAsync()
    {
        var userId = Guid.NewGuid();
        var supabaseSub = $"sub-{userId}";
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(SuperuserConnectionString).Options;
        await using var dbContext = new CommitAheadDbContext(options);
        await new UserRepository(dbContext).AddAsync(new User(userId, supabaseSub, $"{supabaseSub}@example.com", DateTime.UtcNow), CancellationToken.None);

        var token = JwtTestTokenFactory.CreateAccessToken(supabaseSub);
        return $"commitahead_access={token}";
    }

    private static CreateStudyItemRequest ValidCreateRequest(string title) => new(
        title,
        StudyItemCategory.Theory,
        Importance: 3,
        InitialMastery: 3,
        Tags: [],
        Details: new TheoryDetailsDto("Summary", [], [], []));

    [Fact]
    public async Task OwnerA_CannotReadOwnerBsStudyItem_ThroughTheRealHttpPipelineWithRealRls()
    {
        var ownerACookie = await CreateAuthenticatedUserCookieAsync();
        var ownerBCookie = await CreateAuthenticatedUserCookieAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var createResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/study-items", ownerBCookie, ValidCreateRequest("Owner B's item"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<StudyItemCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getAsOwnerA = await client.SendGetAsync($"/api/study-items/{created!.Id}", ownerACookie);
        Assert.Equal(HttpStatusCode.NotFound, getAsOwnerA.StatusCode);

        // Also prove B still sees it — the 404 above is real owner isolation, not a fixture bug
        // that hides the item from everyone.
        var getAsOwnerB = await client.SendGetAsync($"/api/study-items/{created.Id}", ownerBCookie);
        Assert.Equal(HttpStatusCode.OK, getAsOwnerB.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToAnOwnerScopedEndpoint_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.GetAsync("/api/study-items");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
