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

// An unmatched request under /api or /auth must get a real 404, not the SPA shell — these
// catch-alls are lower-priority than any specific controller route but higher-priority than
// MapFallbackToFile below (fallback routes always match last). AllowAnonymous is required on all
// three: the secure-by-default fallback authorization policy (AuthorizationOptions.FallbackPolicy)
// applies not only to matched endpoints without their own [Authorize]/[AllowAnonymous], but also
// to requests that match no endpoint at all — without AllowAnonymous here, unmatched /api or
// /auth requests would 401 instead of 404, and an anonymous visitor could never load index.html
// to see the login form in the first place.
app.Map("/api/{**catchall}", () => Results.NotFound()).AllowAnonymous();
app.Map("/auth/{**catchall}", () => Results.NotFound()).AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

public partial class Program;
