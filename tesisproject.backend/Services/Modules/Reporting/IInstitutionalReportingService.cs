using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

public interface IInstitutionalReportingService
{
    Task<ReportingHealthDto> GetHealthAsync(CancellationToken ct = default);
    Task<InstitutionalReportingDashboardDto> GetDashboardAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
    Task<AuthorReportingDashboardDto> GetAuthorDashboardAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
    Task<byte[]> GenerateDashboardPdfAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
    Task<byte[]> GenerateDashboardPdfAsync(InstitutionalPdfReportRequestDto request, CancellationToken ct = default);
    Task<byte[]> GenerateAuthorPdfAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
    Task<byte[]> GenerateDashboardExcelAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default);
    Task<ReportingHealthDto> RunFullLoadAsync(CancellationToken ct = default);
}
