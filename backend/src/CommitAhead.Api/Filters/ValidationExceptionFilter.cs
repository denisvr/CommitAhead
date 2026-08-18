using CommitAhead.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CommitAhead.Api.Filters;

/// <summary>
/// Maps DomainValidationException — and only that type, deliberately not a blanket Exception
/// catch — to 422, per docs/testing/strategy.md's "malformed payloads, missing required fields,
/// out-of-range values -> 422" convention. Malformed JSON and model-binding failures never reach
/// here — [ApiController]'s automatic model validation already returns 400 for those before an
/// action body runs. Any OTHER exception type, including a plain ArgumentException a library or
/// framework call happened to throw, is a genuine bug or infrastructure failure, not a client
/// input problem — it must propagate to the default unhandled-exception behaviour (500, generic
/// ProblemDetails) rather than being reinterpreted as a validation error and risk exposing a
/// message that was never written to be client-facing. Registered once in Program.cs so no action
/// needs its own try/catch for this.
/// </summary>
public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var message = context.Exception switch
        {
            DomainValidationException ex => ex.Message,
            _ => null,
        };

        if (message is null)
        {
            return;
        }

        context.Result = new UnprocessableEntityObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed.",
            Detail = message,
        });
        context.ExceptionHandled = true;
    }
}
