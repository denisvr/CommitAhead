using System.Text;
using CommitAhead.Api.Identity;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
                    var authority = $"{supabaseUrl}/auth/v1";
                    var requireHttpsMetadata = supabaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    options.Authority = authority;
                    options.Audience = "authenticated";
                    options.RequireHttpsMetadata = requireHttpsMetadata;

                    // GoTrue's own discovery document always advertises `jwks_uri` as its own fixed
                    // self-referential URL (e.g. http://127.0.0.1:54321/... for a local `supabase
                    // start` instance, since its external_url is never configured per-consumer) —
                    // unreachable from inside the api container, which reaches this same instance via
                    // host.docker.internal, not 127.0.0.1 (that address means the container itself
                    // there). Confirmed empirically: the discovery fetch itself succeeds, but the
                    // follow-up jwks_uri fetch then fails with "Connection refused (127.0.0.1:54321)".
                    // LocalSupabaseOpenIdConfigurationRetriever below still uses the discovery
                    // document for Issuer (a fixed string, safe to trust regardless of which address
                    // reached it) but refetches signing keys from OUR OWN configured `authority`
                    // instead of trusting the document's jwks_uri. For a real Cloud Authority
                    // (https), Supabase always sets its own external_url to that same public URL, so
                    // this never diverges from the default OIDC behaviour there.
                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        $"{authority}/.well-known/openid-configuration",
                        new LocalSupabaseOpenIdConfigurationRetriever(authority),
                        new HttpDocumentRetriever { RequireHttps = requireHttpsMetadata });
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

    /// <summary>
    /// Trusts the discovery document's Issuer (a fixed string, valid regardless of which address
    /// reached it) but refetches signing keys from the caller's own known-reachable `authority`
    /// instead of the document's self-reported `jwks_uri` — see the comment where this is
    /// constructed for why that matters for a locally-run Supabase instance reached through
    /// Docker's `host.docker.internal`.
    /// </summary>
    private sealed class LocalSupabaseOpenIdConfigurationRetriever(string authority) : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var discoveryDocument = await retriever.GetDocumentAsync(address, cancel);
            var configuration = new OpenIdConnectConfiguration(discoveryDocument);

            var jwksDocument = await retriever.GetDocumentAsync($"{authority}/.well-known/jwks.json", cancel);
            foreach (var signingKey in new JsonWebKeySet(jwksDocument).GetSigningKeys())
            {
                configuration.SigningKeys.Add(signingKey);
            }

            return configuration;
        }
    }
}
