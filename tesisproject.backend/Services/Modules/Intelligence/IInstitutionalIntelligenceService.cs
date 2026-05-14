using tesisproject.shared.DTOs.Intelligence;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Intelligence;

public interface IInstitutionalIntelligenceService
{
    Task<InstitutionalIntelligenceDashboardDto> GetDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default);

    Task<IntelligenceTrainingRunDto> RunTrainingAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<IntelligenceTrainingRunDto>> GetTrainingHistoryAsync(
        int take = 10,
        CancellationToken ct = default);
}
