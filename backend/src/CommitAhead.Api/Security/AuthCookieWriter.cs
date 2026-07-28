using CommitAhead.Application.Auth;

namespace CommitAhead.Api.Security;

internal static class AuthCookieWriter
{
    public static void SetSessionCookies(HttpResponse response, SupabaseTokenResult tokens)
    {
        response.Cookies.Append(AuthCookieNames.AccessToken, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.AccessTokenExpiresAtUtc,
        });

        response.Cookies.Append(AuthCookieNames.RefreshToken, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/auth/refresh",
            MaxAge = TimeSpan.FromDays(7),
        });
    }

    public static void SetSessionStartedMarker(HttpResponse response, string protectedStartedAtUtc)
    {
        response.Cookies.Append(AuthCookieNames.SessionStarted, protectedStartedAtUtc, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(7),
        });
    }

    public static void ClearSessionCookies(HttpResponse response)
    {
        response.Cookies.Delete(AuthCookieNames.AccessToken);
        response.Cookies.Delete(AuthCookieNames.RefreshToken, new CookieOptions { Path = "/auth/refresh" });
        response.Cookies.Delete(AuthCookieNames.SessionStarted);
    }

    public static void ClearPkceStateCookie(HttpResponse response)
    {
        response.Cookies.Delete(AuthCookieNames.PkceState);
    }
}
