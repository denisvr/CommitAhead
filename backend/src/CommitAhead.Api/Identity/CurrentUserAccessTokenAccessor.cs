using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Http;

namespace CommitAhead.Api.Identity;

/// <summary>Reads the current request's own Supabase access token straight from its cookie (ADR-0018) — the same JWT AuthenticationServiceCollectionExtensions already validated to authenticate this request.</summary>
internal sealed class CurrentUserAccessTokenAccessor : ICurrentUserAccessToken
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessTokenAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Value =>
        _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieNames.AccessToken]
        ?? throw new InvalidOperationException("No access token cookie present on the current request.");
}
