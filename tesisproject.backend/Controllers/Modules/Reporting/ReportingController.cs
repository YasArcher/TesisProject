using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Controllers.Modules.Reporting;

[ApiController]
[Route("api/reporting")]
[Route("api/scientific-production/reporting")]
[Authorize(Policy = AppPolicies.ReportingAccess)]
public sealed class ReportingController : ControllerBase
{
    private readonly IInstitutionalReportingService _reporting;
    private readonly IReportingPerformanceMetricsService _performance;

    public ReportingController(
        IInstitutionalReportingService reporting,
        IReportingPerformanceMetricsService performance)
    {
        _reporting = reporting;
        _performance = performance;
    }

    [HttpGet("health")]
    public async Task<ActionResult<ReportingHealthDto>> GetHealth(CancellationToken ct)
    {
        var result = await _reporting.GetHealthAsync(ct);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<InstitutionalReportingDashboardDto>> GetDashboard(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        return await TrackReportingActionAsync(
            "DashboardLoad",
            filter,
            async () =>
            {
                var result = await _reporting.GetDashboardAsync(filter, ct);
                return (result, (ActionResult<InstitutionalReportingDashboardDto>)Ok(result));
            },
            resultCount: value => value.ScientificProduction.TotalArticles);
    }

    [HttpGet("authors")]
    public async Task<ActionResult<AuthorReportingDashboardDto>> GetAuthors(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        return await TrackReportingActionAsync(
            "AuthorDashboardLoad",
            filter,
            async () =>
            {
                var result = await _reporting.GetAuthorDashboardAsync(filter, ct);
                return (result, (ActionResult<AuthorReportingDashboardDto>)Ok(result));
            },
            resultCount: value => value.Kpis.TotalAuthors);
    }

    [HttpGet("dashboard/pdf")]
    public async Task<IActionResult> GetDashboardPdf(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "DashboardPdf",
            filter,
            async () =>
            {
                var bytes = await _reporting.GenerateDashboardPdfAsync(filter, ct);
                return (bytes, File(bytes, "application/pdf", $"reporte-institucional-{DateTime.UtcNow:yyyyMMddHHmm}.pdf"));
            });
    }

    [HttpPost("dashboard/pdf")]
    public async Task<IActionResult> PostDashboardPdf(
        [FromBody] InstitutionalPdfReportRequestDto request,
        CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "DashboardPdfComposed",
            request.Filter,
            async () =>
            {
                var bytes = await _reporting.GenerateDashboardPdfAsync(request, ct);
                return (bytes, File(bytes, "application/pdf", $"reporte-institucional-{DateTime.UtcNow:yyyyMMddHHmm}.pdf"));
            });
    }

    [HttpGet("authors/pdf")]
    public async Task<IActionResult> GetAuthorsPdf(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "AuthorPdf",
            filter,
            async () =>
            {
                var bytes = await _reporting.GenerateAuthorPdfAsync(filter, ct);
                return (bytes, File(bytes, "application/pdf", $"reporte-autores-{DateTime.UtcNow:yyyyMMddHHmm}.pdf"));
            });
    }

    [HttpGet("dashboard/excel")]
    public async Task<IActionResult> GetDashboardExcel(
        [FromQuery] InstitutionalReportingFilterDto filter,
        CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "DashboardExcel",
            filter,
            async () =>
            {
                var bytes = await _reporting.GenerateDashboardExcelAsync(filter, ct);
                return (bytes, File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"reporte-institucional-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx"));
            });
    }

    [HttpGet("dataset/excel")]
    public async Task<IActionResult> GetRawDatasetExcel(CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "RawDatasetExcel",
            new InstitutionalReportingFilterDto(),
            async () =>
            {
                var bytes = await _reporting.GenerateRawDatasetExcelAsync(ct);
                return (bytes, File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"dataset-reporteria-dide-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx"));
            });
    }

    [HttpGet("dataset/csv")]
    public async Task<IActionResult> GetRawDatasetCsv(CancellationToken ct)
    {
        return await TrackFileActionAsync(
            "RawDatasetCsv",
            new InstitutionalReportingFilterDto(),
            async () =>
            {
                var bytes = await _reporting.GenerateRawDatasetCsvZipAsync(ct);
                return (bytes, File(
                    bytes,
                    "application/zip",
                    $"dataset-reporteria-dide-{DateTime.UtcNow:yyyyMMddHHmm}.zip"));
            });
    }

    [HttpGet("performance")]
    public async Task<ActionResult<ReportingPerformanceSummaryDto>> GetPerformanceSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var result = await _performance.GetSummaryAsync(from, to, ct);
        return Ok(result);
    }

    [HttpPost("etl/full")]
    public async Task<ActionResult<ReportingHealthDto>> RunFullLoad(CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var result = await _reporting.RunFullLoadAsync(ct);
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                "ReportingFullEtl",
                startedAt,
                DateTime.UtcNow,
                true,
                new InstitutionalReportingFilterDto(),
                ResultCount: result.ArticleRows),
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                "ReportingFullEtl",
                startedAt,
                DateTime.UtcNow,
                false,
                new InstitutionalReportingFilterDto(),
                ErrorMessage: ex.Message),
                ct);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = $"No fue posible ejecutar la actualización de reportería: {ex.Message}"
            });
        }
    }

    private async Task<ActionResult<T>> TrackReportingActionAsync<T>(
        string operation,
        InstitutionalReportingFilterDto filter,
        Func<Task<(T Value, ActionResult<T> Result)>> action,
        Func<T, int?>? resultCount = null)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var (value, result) = await action();
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                operation,
                startedAt,
                DateTime.UtcNow,
                true,
                filter,
                resultCount?.Invoke(value)));
            return result;
        }
        catch (Exception ex)
        {
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                operation,
                startedAt,
                DateTime.UtcNow,
                false,
                filter,
                ErrorMessage: ex.Message));
            throw;
        }
    }

    private async Task<IActionResult> TrackFileActionAsync(
        string operation,
        InstitutionalReportingFilterDto filter,
        Func<Task<(byte[] Bytes, IActionResult Result)>> action)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var (bytes, result) = await action();
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                operation,
                startedAt,
                DateTime.UtcNow,
                true,
                filter,
                PayloadBytes: bytes.LongLength));
            return result;
        }
        catch (Exception ex)
        {
            await _performance.RecordAsync(new ReportingPerformanceRecord(
                operation,
                startedAt,
                DateTime.UtcNow,
                false,
                filter,
                ErrorMessage: ex.Message));
            throw;
        }
    }
}
