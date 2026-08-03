using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CommitAhead.Api.Filters;

/// <summary>
/// Maps an ArgumentException (or subclass, e.g. ArgumentOutOfRangeException) thrown by domain or
/// application validation to 422, per docs/testing/strategy.md's "malformed payloads, missing
/// required fields, out-of-range values -> 422" convention. Malformed JSON and model-binding
/// failures never reach here — [ApiController]'s automatic model validation already returns 400
/// for those before an action body runs. Registered once in Program.cs so no action needs its
/// own try/catch for this.
/// </summary>
public sealed class DomainValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ArgumentException exception)
        {
            return;
        }

        context.Result = new UnprocessableEntityObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed.",
            Detail = exception.Message,
        });
        context.ExceptionHandled = true;
    }
}
