using CommitAhead.Api.Identity;
using CommitAhead.Application.Identity;

namespace CommitAhead.Api.Security;

/// <summary>
/// ADR-0015: a validated Supabase JWT is not enough — `sub` must resolve to an existing, enabled
/// application User. Runs after authentication, before authorization, on every request; only
/// acts when the request is authenticated, so anonymous endpoints are unaffected.
/// </summary>
internal sealed class EnabledUserMiddleware
{
    private readonly RequestDelegate _next;

    public EnabledUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, CurrentUserAccessor currentUser)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var supabaseUserId = context.User.FindFirst("sub")?.Value;
            var user = supabaseUserId is null
                ? null
                : await userRepository.GetBySupabaseUserIdAsync(supabaseUserId, context.RequestAborted);

            if (user is null || !user.IsEnabled)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            currentUser.Set(user.Id, user.Email);
        }

        await _next(context);
    }
}
