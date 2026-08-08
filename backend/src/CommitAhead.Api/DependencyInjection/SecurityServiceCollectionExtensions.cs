using System.Threading.RateLimiting;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.DependencyInjection;

public static class SecurityServiceCollectionExtensions
{
    private const string DevCorsPolicy = "DevFrontend";

    public static IServiceCollection AddCommitAheadSecurity(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // Durable/encrypted key storage for production is a Phase 6 decision (docs/tbd.md) — the
        // default key ring is enough to seal the session-start timestamp for local/dev use now.
        services.AddDataProtection();
        services.AddSingleton<SessionStartToken>();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "commitahead_csrf";
            options.Cookie.HttpOnly = false; // the SPA must read this to echo it back as a header
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(15),
                        PermitLimit = 5,
                        QueueLimit = 0,
                    }));

            // Per-owner, not per-IP (ADR-0019/ADR-0015) — the partition key needs the authenticated
            // identity, so this policy only works correctly evaluated after UseAuthentication()/
            // UseAuthorization() in the pipeline (Program.cs). Applied only to the three AnalyzeX
            // "analyze" actions, never to ApplyAnalysisDraft.
            options.AddPolicy("ai-analysis", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.RequestServices.GetRequiredService<ICurrentUser>().UserId.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 10,
                        QueueLimit = 0,
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsync("Rate limit exceeded.", cancellationToken);
            };
        });

        if (environment.IsDevelopment())
        {
            services.AddCors(options =>
            {
                options.AddPolicy(DevCorsPolicy, policy => policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });
        }

        return services;
    }

    public static IApplicationBuilder UseCommitAheadCors(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        return environment.IsDevelopment() ? app.UseCors(DevCorsPolicy) : app;
    }
}
