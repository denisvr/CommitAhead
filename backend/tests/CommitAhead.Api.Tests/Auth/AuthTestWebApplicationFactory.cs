using System.Text;
using CommitAhead.Application.Auth;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CommitAhead.Api.Tests.Auth;

/// <summary>
/// Shared factory for every auth-related API test: replaces IUserRepository and
/// ISupabaseAuthClient with in-memory stubs (no live Postgres, no real Supabase calls), and
/// points JWT validation at a fixed local signing key instead of Supabase's real JWKS.
/// </summary>
public sealed class AuthTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestIssuer = "https://test.supabase.local/auth/v1";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("test-only-signing-key-at-least-32-bytes-long"));

    public StubUserRepository Users { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidIssuer = TestIssuer;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = "authenticated";
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            });

            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(Users);

            services.RemoveAll<ISupabaseAuthClient>();
            services.AddSingleton<ISupabaseAuthClient, StubSupabaseAuthClient>();
        });
    }
}
