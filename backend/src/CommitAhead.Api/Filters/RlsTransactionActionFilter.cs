using System.Runtime.ExceptionServices;
using CommitAhead.Api.Security;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CommitAhead.Api.Filters;

/// <summary>
/// Wraps [UsesOwnerScopedData] actions in an RLS owner scope (IRlsSessionContext) so the Phase 1
/// owner-isolation policies (docs/architecture/persistence.md "Supabase RLS") see the right
/// current_setting('app.current_user_id') for the duration of the action — and, critically, so the
/// transaction commits here, as part of the action stage, strictly before the result stage writes
/// any response bytes. A middleware wrapping the whole request (the previous design) would commit
/// only after the response had already started being written, which can hand a client a "success"
/// for a write that has not actually persisted yet.
///
/// An action filter's `next()` does not throw when the action itself throws — it returns an
/// ActionExecutedContext with .Exception set — so this must check that explicitly and re-throw to
/// make IRlsSessionContext's own try/rollback logic (which reacts to a thrown exception) actually
/// roll back instead of silently committing a failed action's partial writes.
/// </summary>
public sealed class RlsTransactionActionFilter : IAsyncActionFilter
{
    private readonly IRlsSessionContext _rlsSessionContext;
    private readonly ICurrentUser _currentUser;

    public RlsTransactionActionFilter(IRlsSessionContext rlsSessionContext, ICurrentUser currentUser)
    {
        _rlsSessionContext = rlsSessionContext;
        _currentUser = currentUser;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var usesOwnerScopedData = context.ActionDescriptor.EndpointMetadata.Any(metadata => metadata is UsesOwnerScopedDataAttribute);
        if (!usesOwnerScopedData || _currentUser.UserId == Guid.Empty)
        {
            await next();
            return;
        }

        await _rlsSessionContext.RunInOwnerScopeAsync(_currentUser.UserId, async () =>
        {
            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                ExceptionDispatchInfo.Capture(executed.Exception).Throw();
            }
        }, context.HttpContext.RequestAborted);
    }
}
