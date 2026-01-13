using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IVisitService
    {
        Task<ServiceResult<VisitListResponseDTO>> CreateAsync(AddVisitRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<VisitListResponseDTO>> GetByIdAsync(int visitId, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<VisitListResponseDTO>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<VisitListResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<VisitListResponseDTO>> UpdateAsync(UpdateVisitRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int visitId, CancellationToken ct = default);
        Task<ServiceResult<VisitDetailResponseDTO>> GetVisitDetailAsync(int visitId, CancellationToken ct = default);
        Task<ServiceResult<VisitListResponseDTO>> FinalizeAsync(FinalizeVisitRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListPlannedForExecutionAsync(bool isFirstVisit, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> BulkScheduleAsync(BulkScheduleVisitsRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListByStateAsync(int visitStateId, CancellationToken ct = default);

    }
}
