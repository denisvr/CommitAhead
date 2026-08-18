using System.Text.Json.Serialization;
using CommitAhead.Api.DependencyInjection;
using CommitAhead.Api.Filters;
using CommitAhead.Api.Security;
using CommitAhead.Application.DependencyInjection;
using CommitAhead.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationExceptionFilter>();
        options.Filters.Add<RlsTransactionActionFilter>();
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Microsoft.AspNetCore.OpenApi's schema generator reads Http.Json.JsonOptions, not MVC's
// JsonOptions above — without this, the OpenAPI document (and the frontend's generated
// TypeScript client) would describe enums as plain numbers while every actual response, per the
// MVC option, serializes them as strings. Both must agree.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Fail-closed, before anything else touches configuration: E2E:* settings must be present if and
// only if this is the E2E environment, and — inside E2E — every external-provider URL/credential
// must equal its exact approved sentinel (E2E Foundation Plan). Safe to run unconditionally,
// including under build-time OpenAPI document generation, which never runs as ASPNETCORE_ENVIRONMENT=E2E.
CommitAhead.Api.Security.E2EConfigurationGuard.Validate(builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCommitAheadAuthentication(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddCommitAheadSecurity(builder.Environment, builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// The "Docker" environment (docker-compose.prod.yml, ADR-0021) is a production-like local
// validation target with no TLS termination of its own — a real deployment puts this behind a
// TLS-terminating reverse proxy/load balancer instead. Sending HSTS or redirecting to https
// without any https listener behind it would just break every request, so both are skipped only
// for this one environment name; every other non-Development environment keeps them. "E2E"
// (docker-compose.e2e.yml) is the same situation — its own reverse proxy terminates nothing and
// forwards plain HTTP — so it is skipped for the identical reason, not merged into "Docker"
// itself: the two stacks are deliberately distinct environments (E2E Foundation Plan).
if (!app.Environment.IsEnvironment("Docker") && !app.Environment.IsEnvironment("E2E"))
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCommitAheadCors(app.Environment);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseMiddleware<CsrfMiddleware>();

// RlsTransactionActionFilter (registered above) opens the RLS owner scope — a global MVC action
// filter, not middleware, so its transaction commits before the result stage writes any response
// bytes. It runs after CSRF (a rejected request never even reaches the action) and relies on
// UseAuthorization() above having already populated ICurrentUser.
app.MapControllers();

// ApiCatchAllController/AuthCatchAllController (Features/Routing) give unmatched /api or /auth
// requests a real 404 instead of falling through to the SPA shell below — Controllers, not
// Minimal APIs, per ADR-0008. MapFallbackToFile itself is the one Minimal-API-shaped registration
// left, and it exists only to serve the React shell for everything else. AllowAnonymous is
// required here: the secure-by-default fallback authorization policy
// (AuthorizationOptions.FallbackPolicy) applies not only to matched endpoints without their own
// [Authorize]/[AllowAnonymous], but also to requests that match no endpoint at all — without it,
// an anonymous visitor could never load index.html to see the login form in the first place.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

public partial class Program;
