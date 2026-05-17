using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

public sealed class ReportingPerformanceMetricsService : IReportingPerformanceMetricsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ReportingPerformanceMetricsService> _logger;

    public ReportingPerformanceMetricsService(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ReportingPerformanceMetricsService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordAsync(ReportingPerformanceRecord record, CancellationToken ct = default)
    {
        try
        {
            var durationMs = Math.Max(0, (long)(record.FinishedAtUtc - record.StartedAtUtc).TotalMilliseconds);
            var baselineMs = GetManualBaselineMs(record.Operation, record.Filter);
            var user = _httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims
                .Where(x => x.Type == ClaimTypes.Role || x.Type == "role")
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            var metric = new ReportingPerformanceMetric
            {
                Operation = record.Operation,
                StartedAtUtc = record.StartedAtUtc,
                FinishedAtUtc = record.FinishedAtUtc,
                DurationMs = durationMs,
                Succeeded = record.Succeeded,
                UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub"),
                UserName = user?.Identity?.Name ?? user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email"),
                Roles = roles.Count == 0 ? null : string.Join(",", roles),
                FilterSummaryJson = BuildFilterSummaryJson(record.Filter),
                ResultCount = record.ResultCount,
                PayloadBytes = record.PayloadBytes,
                ManualBaselineMs = baselineMs,
                EstimatedTimeSavedMs = Math.Max(0, baselineMs - durationMs),
                ErrorMessage = string.IsNullOrWhiteSpace(record.ErrorMessage) ? null : record.ErrorMessage[..Math.Min(record.ErrorMessage.Length, 1000)],
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.ReportingPerformanceMetrics.Add(metric);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible registrar la métrica de desempeño de reportería. Operación={Operation}.", record.Operation);
        }
    }

    public async Task<ReportingPerformanceSummaryDto> GetSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        try
        {
            var fromUtc = from?.ToUniversalTime();
            var toUtc = to?.ToUniversalTime();
            var query = _db.ReportingPerformanceMetrics.AsNoTracking();

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.StartedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.StartedAtUtc < toUtc.Value);
            }

            var rows = await query
                .OrderByDescending(x => x.StartedAtUtc)
                .Take(5000)
                .ToListAsync(ct);

            return BuildSummary(rows);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible leer las métricas de desempeño de reportería.");
            return new ReportingPerformanceSummaryDto { GeneratedAt = DateTime.Now };
        }
    }

    private static ReportingPerformanceSummaryDto BuildSummary(List<ReportingPerformanceMetric> rows)
    {
        var successful = rows.Where(x => x.Succeeded).ToList();
        var totalDuration = successful.Sum(x => x.DurationMs);
        var totalBaseline = successful.Sum(x => x.ManualBaselineMs);
        var totalSaved = successful.Sum(x => x.EstimatedTimeSavedMs);

        return new ReportingPerformanceSummaryDto
        {
            GeneratedAt = DateTime.Now,
            TotalOperations = rows.Count,
            SuccessfulOperations = successful.Count,
            FailedOperations = rows.Count - successful.Count,
            AverageDurationSeconds = AverageMsAsSeconds(successful.Select(x => x.DurationMs)),
            AverageEstimatedManualMinutes = AverageMsAsMinutes(successful.Select(x => x.ManualBaselineMs)),
            AverageEstimatedSavedMinutes = AverageMsAsMinutes(successful.Select(x => x.EstimatedTimeSavedMs)),
            EstimatedReductionPercent = totalBaseline <= 0 ? 0 : Math.Round(totalSaved * 100m / totalBaseline, 2),
            Operations = successful
                .GroupBy(x => x.Operation)
                .Select(g =>
                {
                    var operationRows = g.ToList();
                    var baseline = operationRows.Sum(x => x.ManualBaselineMs);
                    var saved = operationRows.Sum(x => x.EstimatedTimeSavedMs);
                    return new ReportingPerformanceOperationDto
                    {
                        Operation = g.Key,
                        TotalOperations = operationRows.Count,
                        SuccessfulOperations = operationRows.Count(x => x.Succeeded),
                        AverageDurationSeconds = AverageMsAsSeconds(operationRows.Select(x => x.DurationMs)),
                        AverageEstimatedManualMinutes = AverageMsAsMinutes(operationRows.Select(x => x.ManualBaselineMs)),
                        EstimatedReductionPercent = baseline <= 0 ? 0 : Math.Round(saved * 100m / baseline, 2)
                    };
                })
                .OrderByDescending(x => x.TotalOperations)
                .ToList(),
            DailyUsage = successful
                .GroupBy(x => x.StartedAtUtc.Date)
                .Select(g => new ReportingPerformanceDailyDto
                {
                    Date = g.Key,
                    TotalOperations = g.Count(),
                    AverageDurationSeconds = AverageMsAsSeconds(g.Select(x => x.DurationMs))
                })
                .OrderBy(x => x.Date)
                .ToList()
        };
    }

    private static decimal AverageMsAsSeconds(IEnumerable<long> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : Math.Round((decimal)list.Average() / 1000m, 2);
    }

    private static decimal AverageMsAsMinutes(IEnumerable<long> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : Math.Round((decimal)list.Average() / 60000m, 2);
    }

    private static long GetManualBaselineMs(string operation, InstitutionalReportingFilterDto? filter)
    {
        var hasFilters = ReportingFilterApplicator.HasScopedInstitutionalFilters(filter)
            || ReportingFilterApplicator.HasAuthorScopedFilters(filter);

        var minutes = operation switch
        {
            "DashboardLoad" => hasFilters ? 25 : 20,
            "AuthorDashboardLoad" => hasFilters ? 30 : 25,
            "DashboardPdf" => 180,
            "DashboardPdfComposed" => 210,
            "AuthorPdf" => 180,
            "DashboardExcel" => 150,
            "RawDatasetExcel" => 240,
            "RawDatasetCsv" => 240,
            _ => hasFilters ? 20 : 15
        };

        return minutes * 60_000L;
    }

    private static string? BuildFilterSummaryJson(InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return null;
        }

        var summary = new Dictionary<string, object?>();
        Add(summary, nameof(filter.CreatedFrom), filter.CreatedFrom);
        Add(summary, nameof(filter.CreatedTo), filter.CreatedTo);
        Add(summary, nameof(filter.PublishedFrom), filter.PublishedFrom);
        Add(summary, nameof(filter.PublishedTo), filter.PublishedTo);
        Add(summary, nameof(filter.ArticleTitle), filter.ArticleTitle);
        Add(summary, nameof(filter.ArticleDoi), filter.ArticleDoi);
        Add(summary, nameof(filter.ProjectName), filter.ProjectName);
        Add(summary, nameof(filter.AcademicTerm), filter.AcademicTerm);
        Add(summary, nameof(filter.PublicationStatus), filter.PublicationStatus);
        Add(summary, nameof(filter.ResearchLine), filter.ResearchLine);
        Add(summary, nameof(filter.Faculty), filter.Faculty);
        Add(summary, nameof(filter.IndexingSource), filter.IndexingSource);
        Add(summary, nameof(filter.BroadField), filter.BroadField);
        Add(summary, nameof(filter.SpecificField), filter.SpecificField);
        Add(summary, nameof(filter.DetailedField), filter.DetailedField);
        Add(summary, nameof(filter.VenueName), filter.VenueName);
        Add(summary, nameof(filter.VenueType), filter.VenueType);
        Add(summary, nameof(filter.ArticleYear), filter.ArticleYear);
        Add(summary, nameof(filter.ArticleMonth), filter.ArticleMonth);
        Add(summary, nameof(filter.Quartile), filter.Quartile);
        Add(summary, nameof(filter.IsOpenAccess), filter.IsOpenAccess);
        Add(summary, nameof(filter.IsProjectResult), filter.IsProjectResult);
        Add(summary, nameof(filter.HasInterculturalComponent), filter.HasInterculturalComponent);
        Add(summary, nameof(filter.AuthorName), filter.AuthorName);
        Add(summary, nameof(filter.AuthorAffiliation), filter.AuthorAffiliation);
        Add(summary, nameof(filter.ParticipantType), filter.ParticipantType);
        Add(summary, nameof(filter.HasOrcid), filter.HasOrcid);
        Add(summary, nameof(filter.OnlyPrimaryAuthors), filter.OnlyPrimaryAuthors);
        Add(summary, nameof(filter.CoauthorName), filter.CoauthorName);

        return summary.Count == 0 ? null : JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static void Add(Dictionary<string, object?> summary, string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        summary[key] = value;
    }
}
