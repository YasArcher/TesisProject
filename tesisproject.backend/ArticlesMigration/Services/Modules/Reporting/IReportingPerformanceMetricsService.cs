using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

public interface IReportingPerformanceMetricsService
{
    Task RecordAsync(ReportingPerformanceRecord record, CancellationToken ct = default);
    Task<ReportingPerformanceSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}

public sealed record ReportingPerformanceRecord(
    string Operation,
    DateTime StartedAtUtc,
    DateTime FinishedAtUtc,
    bool Succeeded,
    InstitutionalReportingFilterDto? Filter,
    int? ResultCount = null,
    long? PayloadBytes = null,
    string? ErrorMessage = null);
