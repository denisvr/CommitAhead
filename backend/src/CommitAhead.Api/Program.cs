using CommitAhead.Api.DependencyInjection;
using CommitAhead.Api.Security;
using CommitAhead.Application.DependencyInjection;
using CommitAhead.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCommitAheadAuthentication(builder.Configuration);
builder.Services.AddCommitAheadSecurity(builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCommitAheadCors(app.Environment);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CsrfMiddleware>();

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
