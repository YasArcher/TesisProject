using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Modules.Intelligence;
using tesisproject.shared.DTOs.Intelligence;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Controllers.Modules.Intelligence;

[ApiController]
[Route("api/intelligence")]
[Authorize(Policy = AppPolicies.ReportingAccess)]
public sealed class IntelligenceController : ControllerBase
{
    private readonly IInstitutionalIntelligenceService _intelligence;

    public IntelligenceController(IInstitutionalIntelligenceService intelligence)
    {
        _intelligence = intelligence;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<InstitutionalIntelligenceDashboardDto>> GetDashboard(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        var result = await _intelligence.GetDashboardAsync(filter, ct);
        return Ok(result);
    }

    [HttpPost("training/run")]
    public async Task<ActionResult<IntelligenceTrainingRunDto>> RunTraining(
        [FromBody] InstitutionalReportingFilterDto? filter,
        CancellationToken ct)
    {
        var result = await _intelligence.RunTrainingAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("training/history")]
    public async Task<ActionResult<IReadOnlyList<IntelligenceTrainingRunDto>>> GetTrainingHistory(
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var result = await _intelligence.GetTrainingHistoryAsync(take, ct);
        return Ok(result);
    }
}
