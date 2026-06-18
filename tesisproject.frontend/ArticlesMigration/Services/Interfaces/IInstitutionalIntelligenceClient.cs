using tesisproject.shared.DTOs.Intelligence;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Interfaces;

public interface IInstitutionalIntelligenceClient
{
    Task<InstitutionalIntelligenceDashboardDto?> GetDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default);

    Task<IntelligenceTrainingRunDto?> RunTrainingAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<IntelligenceTrainingRunDto>> GetTrainingHistoryAsync(
        int take = 10,
        CancellationToken ct = default);
}
