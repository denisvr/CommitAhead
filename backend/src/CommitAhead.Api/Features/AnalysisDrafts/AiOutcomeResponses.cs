using CommitAhead.Application.AI;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.AnalysisDrafts;

/// <summary>
/// Shared response-shaping for AI-command outcomes that aren't a plain 200/201/404 — a stable,
/// machine-readable "outcomeCode" in ProblemDetails.Extensions (never text embedded in Detail, so
/// the frontend never has to parse prose) plus, for budget outcomes, a computed Retry-After.
/// </summary>
internal static class AiOutcomeResponses
{
    /// <summary>
    /// <paramref name="analysisDraftId"/> is set only for DraftAlreadyPending — the one Conflict
    /// outcome with a real draft to recover (a caller that lost track of it, e.g. a refresh, can
    /// still navigate back to review it). ASP.NET Core serializes ProblemDetails.Extensions entries
    /// as root-level JSON properties, not nested under an "extensions" object.
    /// </summary>
    public static ObjectResult Conflict(string outcomeCode, Guid? analysisDraftId = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The request could not be completed in the draft's current state.",
            Extensions = { ["outcomeCode"] = outcomeCode },
        };

        if (analysisDraftId is not null)
        {
            problemDetails.Extensions["analysisDraftId"] = analysisDraftId;
        }

        return new ObjectResult(problemDetails) { StatusCode = StatusCodes.Status409Conflict };
    }

    public static ObjectResult BudgetExceeded(AnalyzeCommandOutcome outcome, HttpResponse response)
    {
        var retryAfterSeconds = outcome == AnalyzeCommandOutcome.DailyBudgetExceeded
            ? SecondsUntilNextUtcDay()
            : SecondsUntilNextUtcMonth();
        response.Headers.RetryAfter = retryAfterSeconds.ToString();

        return new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "The AI budget for this owner has been exceeded.",
            Extensions = { ["outcomeCode"] = outcome.ToString() },
        })
        { StatusCode = StatusCodes.Status429TooManyRequests };
    }

    private static long SecondsUntilNextUtcDay()
    {
        var nowUtc = DateTime.UtcNow;
        return (long)(nowUtc.Date.AddDays(1) - nowUtc).TotalSeconds;
    }

    private static long SecondsUntilNextUtcMonth()
    {
        var nowUtc = DateTime.UtcNow;
        var nextMonthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return (long)(nextMonthStartUtc - nowUtc).TotalSeconds;
    }
}
