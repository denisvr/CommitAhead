using System.Text;
using CommitAhead.Api.Identity;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CommitAhead.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    // The access token cookie's own MaxAge already caps at this value (see AuthCookieWriter), but
    // that only stops a real browser from resending it. OnTokenValidated enforces the same limit
    // server-side against the token's `iat` claim, so a raw replay of a captured cookie value
    // past its intended lifetime is rejected regardless of client behaviour.
    private static readonly TimeSpan AccessTokenEffectiveLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(1);

    private const string E2EEnvironmentName = "E2E";

    public static IServiceCollection AddCommitAheadAuthentication(this IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        // Read lazily rather than throwing here: this method runs whenever Program's entry point
        // runs, including build-time OpenAPI document generation, which never loads user-secrets.
        // An unconfigured Supabase:Url simply means every request fails JWT validation (401) —
        // discoverable at runtime, not a build break.
        var supabaseUrl = configuration["Supabase:Url"];
        var isE2E = string.Equals(environmentName, E2EEnvironmentName, StringComparison.Ordinal);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // The E2E stack can never reach Supabase's real JWKS (no route off its internal
                // network — docs/testing/strategy.md §7.6), so JWT validation is pointed at a
                // fixed local signing key instead of an Authority, mirroring the existing
                // AuthTestWebApplicationFactory precedent but as real configuration rather than a
                // WebApplicationFactory override. E2EConfigurationGuard has already verified
                // E2E:SigningKey/Issuer are present whenever isE2E is true.
                if (isE2E)
                {
                    var e2eSigningKey = configuration["E2E:SigningKey"]!;
                    var e2eIssuer = configuration["E2E:Issuer"]!;

                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidIssuer = e2eIssuer;
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidAudience = "authenticated";
                    options.TokenValidationParameters.ValidateLifetime = true;
                    options.TokenValidationParameters.RequireExpirationTime = true;
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(e2eSigningKey));
                }
                else if (!string.IsNullOrWhiteSpace(supabaseUrl))
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

        // No .ValidateOnStart(): same lazy-failure posture as every other options type in this
        // codebase (build-time OpenAPI generation runs the host without user-secrets). Presence
        // and correctness of these values is instead enforced eagerly by
        // E2EConfigurationGuard.Validate, called directly from Program.cs before the pipeline is
        // built — a real fail-closed startup guard, not merely deferred to first use.
        services.AddOptions<E2EOptions>().Bind(configuration.GetSection(E2EOptions.SectionName));

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
