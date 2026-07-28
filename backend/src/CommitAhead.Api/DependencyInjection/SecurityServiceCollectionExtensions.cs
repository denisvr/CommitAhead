using System.Threading.RateLimiting;
using CommitAhead.Api.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.DependencyInjection;

public static class SecurityServiceCollectionExtensions
{
    private const string DevCorsPolicy = "DevFrontend";

    public static IServiceCollection AddCommitAheadSecurity(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "commitahead_csrf";
            options.Cookie.HttpOnly = false; // the SPA must read this to echo it back as a header
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(15),
                        PermitLimit = 5,
                        QueueLimit = 0,
                    }));
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
