using tesisproject.frontend.Services.Implementations;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Intelligence;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Implementations;

public sealed class InstitutionalIntelligenceClient : IInstitutionalIntelligenceClient
{
    private readonly IApiClient _apiClient;

    public InstitutionalIntelligenceClient(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<InstitutionalIntelligenceDashboardDto?> GetDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        return _apiClient.GetAsync<InstitutionalIntelligenceDashboardDto>(
            InstitutionalReportingClient.BuildDashboardUrl(filter, "api/intelligence/dashboard"),
            ct);
    }

    public Task<IntelligenceTrainingRunDto?> RunTrainingAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        return _apiClient.PostAsync<InstitutionalReportingFilterDto, IntelligenceTrainingRunDto>(
            "api/intelligence/training/run",
            filter ?? new InstitutionalReportingFilterDto(),
            ct);
    }

    public async Task<IReadOnlyList<IntelligenceTrainingRunDto>> GetTrainingHistoryAsync(
        int take = 10,
        CancellationToken ct = default)
    {
        return await _apiClient.GetAsync<List<IntelligenceTrainingRunDto>>(
            $"api/intelligence/training/history?take={take}",
            ct) ?? [];
    }
}
