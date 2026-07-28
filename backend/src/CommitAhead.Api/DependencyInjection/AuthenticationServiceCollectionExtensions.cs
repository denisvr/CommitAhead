using CommitAhead.Api.Identity;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CommitAhead.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
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
                };
            });

        services.AddAuthorization();

        services.AddScoped<CurrentUserAccessor>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserAccessor>());

        return services;
    }
}
