using CommitAhead.Api.Security;
using CommitAhead.Application.StudyItems;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.StudyItems;

[ApiController]
[Route("api/scoring-config")]
[UsesOwnerScopedData]
public sealed class ScoringConfigController : ControllerBase
{
    private readonly GetScoringConfigUseCase _getUseCase;
    private readonly UpdateScoringConfigUseCase _updateUseCase;
    private readonly ResetScoringConfigUseCase _resetUseCase;

    public ScoringConfigController(GetScoringConfigUseCase getUseCase, UpdateScoringConfigUseCase updateUseCase, ResetScoringConfigUseCase resetUseCase)
    {
        _getUseCase = getUseCase;
        _updateUseCase = updateUseCase;
        _resetUseCase = resetUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ScoringConfigResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(cancellationToken);
        return Ok(new ScoringConfigResponse(result.ImportanceWeight, result.DemandWeight, result.MasteryGapWeight, result.IsOverridden));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UpdateScoringConfigRequest request, CancellationToken cancellationToken)
    {
        // ScoringWeights' constructor validates non-negativity and the sum-to-100 invariant and
        // throws ArgumentException; ValidationExceptionFilter maps that to 422 for every
        // controller in this API, so no local try/catch is needed here.
        await _updateUseCase.ExecuteAsync(request.ImportanceWeight, request.DemandWeight, request.MasteryGapWeight, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await _resetUseCase.ExecuteAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record ScoringConfigResponse(int ImportanceWeight, int DemandWeight, int MasteryGapWeight, bool IsOverridden);

public sealed record UpdateScoringConfigRequest(int ImportanceWeight, int DemandWeight, int MasteryGapWeight);
