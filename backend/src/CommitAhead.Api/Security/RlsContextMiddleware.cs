using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;

namespace CommitAhead.Api.Security;

/// <summary>
/// Wraps requests to [UsesOwnerScopedData] endpoints in an RLS owner scope (IRlsSessionContext) so
/// the Phase 1 owner-isolation policies (docs/architecture/persistence.md "Supabase RLS") see the
/// right current_setting('app.current_user_id') for the duration of the request. Must run after
/// UseAuthorization() — CurrentUserAccessor is populated there — and after CsrfMiddleware, so a
/// CSRF-rejected request never opens a transaction. Endpoints without the attribute (health,
/// /api/me, every [AllowAnonymous] auth endpoint) pass through untouched — they never read the
/// owner-scoped tables this exists for, and the only table auth endpoints DO read (users) is
/// unconditionally granted to commitahead_app regardless of any session-local setting.
/// </summary>
internal sealed class RlsContextMiddleware
{
    private readonly RequestDelegate _next;

    public RlsContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, IRlsSessionContext rlsSessionContext, ICurrentUser currentUser)
    {
        var usesOwnerScopedData = context.GetEndpoint()?.Metadata.GetMetadata<UsesOwnerScopedDataAttribute>() is not null;
        if (!usesOwnerScopedData || currentUser.UserId == Guid.Empty)
        {
            return _next(context);
        }

        return rlsSessionContext.RunInOwnerScopeAsync(currentUser.UserId, () => _next(context), context.RequestAborted);
    }
}
