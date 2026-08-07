using CommitAhead.Api.Identity;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace CommitAhead.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    // The access token cookie's own MaxAge already caps at this value (see AuthCookieWriter), but
    // that only stops a real browser from resending it. OnTokenValidated enforces the same limit
    // server-side against the token's `iat` claim, so a raw replay of a captured cookie value
    // past its intended lifetime is rejected regardless of client behaviour.
    private static readonly TimeSpan AccessTokenEffectiveLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddCommitAheadAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Read lazily rather than throwing here: this method runs whenever Program's entry point
        // runs, including build-time OpenAPI document generation, which never loads user-secrets.
        // An unconfigured Supabase:Url simply means every request fails JWT validation (401) —
        // discoverable at runtime, not a build break.
        var supabaseUrl = configuration["Supabase:Url"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(supabaseUrl))
                {
                    options.Authority = $"{supabaseUrl}/auth/v1";
                    options.Audience = "authenticated";
                }

                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var issuedAtClaim = context.Principal?.FindFirst("iat")?.Value;
                        if (issuedAtClaim is null || !long.TryParse(issuedAtClaim, out var issuedAtUnix))
                        {
                            context.Fail("Missing or invalid iat claim.");
                            return Task.CompletedTask;
                        }

                        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);
                        if (DateTimeOffset.UtcNow - issuedAt > AccessTokenEffectiveLifetime + ClockSkewTolerance)
                        {
                            context.Fail("Access token exceeds the 15-minute effective limit.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddHttpContextAccessor();

        services.AddAuthorization(options =>
        {
            // Secure by default: every endpoint requires authentication AND an enabled ADR-0015
            // user unless it explicitly opts out with [AllowAnonymous] (health and auth endpoints
            // only — AllowAnonymous skips this policy entirely, so login/callback/refresh/logout/
            // csrf never evaluate EnabledUserRequirement). DefaultPolicy is set identically so a
            // bare [Authorize] (which uses DefaultPolicy, not FallbackPolicy) still enforces the
            // same check instead of silently bypassing it.
            var protectedResourcePolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new EnabledUserRequirement())
                .Build();

            options.FallbackPolicy = protectedResourcePolicy;
            options.DefaultPolicy = protectedResourcePolicy;
        });

        services.AddScoped<IAuthorizationHandler, EnabledUserAuthorizationHandler>();

        services.AddScoped<CurrentUserAccessor>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserAccessor>());
        services.AddScoped<ICurrentUserAccessToken, CurrentUserAccessTokenAccessor>();

        return services;
    }
}
