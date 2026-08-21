using System.Threading.RateLimiting;
using CommitAhead.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.DependencyInjection;

public static class SecurityServiceCollectionExtensions
{
    private const string DevCorsPolicy = "DevFrontend";

    public static IServiceCollection AddCommitAheadSecurity(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var dataProtectionBuilder = services.AddDataProtection();

        // "DataProtection:KeyRingPath" (env form DataProtection__KeyRingPath) points the key ring
        // at a directory backed by a named volume in docker-compose.prod.yml, so cookie-encryption
        // keys survive a container restart instead of invalidating every session (ADR-0021). Left
        // unset, AddDataProtection() falls back to its own per-environment default (ephemeral for
        // local `dotnet run`/tests) — encrypting the key ring at rest with a cloud KMS is still the
        // open decision in docs/tbd.md ("Data Protection key ring storage"), not resolved here.
        var keyRingPath = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }

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

            options.AddPolicy("export", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveCallerPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TransportLimits.ExportWindow,
                        PermitLimit = TransportLimits.ExportsPerWindow,
                        QueueLimit = 0,
                    }));

            // A global limiter rather than an attribute per mutating action: a new state-changing
            // endpoint cannot forget to opt in, which is the failure mode an attribute has and this
            // does not. Safe methods are exempt so ordinary page loads are never throttled, and
            // static files never reach here at all (UseStaticFiles runs before the limiter).
            // Endpoint policies stack on top of this rather than replacing it, so CV export is
            // bounded by both.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsStateChanging(context.Request.Method)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"write:{ResolveCallerPartitionKey(context)}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TransportLimits.WriteWindow,
                            PermitLimit = TransportLimits.WritesPerMinute,
                            QueueLimit = 0,
                        })
                    : RateLimitPartition.GetNoLimiter("safe-method"));

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

    private static bool IsStateChanging(string method)
    {
        return !HttpMethods.IsGet(method)
            && !HttpMethods.IsHead(method)
            && !HttpMethods.IsOptions(method)
            && !HttpMethods.IsTrace(method);
    }

    /// <summary>
    /// Partitions by the authenticated subject so one caller cannot spend another caller's budget,
    /// and falls back to the remote address only for anonymous requests. The claim is "sub" rather
    /// than ClaimTypes.NameIdentifier because MapInboundClaims is disabled (see
    /// AuthenticationServiceCollectionExtensions), so inbound claim names are never rewritten.
    /// </summary>
    private static string ResolveCallerPartitionKey(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value;

        return string.IsNullOrEmpty(subject)
            ? $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"
            : $"sub:{subject}";
    }
}
