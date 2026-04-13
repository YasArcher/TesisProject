using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Controllers.Modules.Reporting;

[ApiController]
[Route("api/reporting")]
[Authorize(Policy = AppPolicies.ReportingAccess)]
public sealed class ReportingController : ControllerBase
{
    private readonly IInstitutionalReportingService _reporting;

    public ReportingController(IInstitutionalReportingService reporting)
    {
        _reporting = reporting;
    }

    [HttpGet("health")]
    public async Task<ActionResult<ReportingHealthDto>> GetHealth(CancellationToken ct)
    {
        var result = await _reporting.GetHealthAsync(ct);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<InstitutionalReportingDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var result = await _reporting.GetDashboardAsync(ct);
        return Ok(result);
    }

    [HttpPost("etl/full")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ReportingHealthDto>> RunFullLoad(CancellationToken ct)
    {
        var result = await _reporting.RunFullLoadAsync(ct);
        return Ok(result);
    }
}
