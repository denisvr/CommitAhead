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
app.UseMiddleware<EnabledUserMiddleware>();
app.UseAuthorization();

app.UseMiddleware<CsrfMiddleware>();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
