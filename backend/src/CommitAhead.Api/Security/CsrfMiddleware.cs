using Microsoft.AspNetCore.Antiforgery;

namespace CommitAhead.Api.Security;

/// <summary>
/// Secure by default: every state-changing request is validated unless the matched endpoint
/// carries [SkipCsrf]. Must run after UseRouting (needs the matched endpoint) and after
/// UseAuthorization (no point validating CSRF on a request authorization would reject anyway).
/// </summary>
internal sealed class CsrfMiddleware
{
    private static readonly HashSet<string> StateChangingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var isExempt = context.GetEndpoint()?.Metadata.GetMetadata<SkipCsrfAttribute>() is not null;

        if (!isExempt && StateChangingMethods.Contains(context.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        }

        await _next(context);
    }
}
