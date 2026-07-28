namespace CommitAhead.Api.Security;

/// <summary>
/// The security headers block from docs/security/threat-model.md, verbatim. HSTS is handled
/// separately by the framework's own UseHsts (production-only, needs its own opt-in).
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] =
                "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' blob:; " +
                "font-src 'self'; connect-src 'self'; manifest-src 'self'; object-src 'none'; " +
                "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
