using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IVisitObjectiveActivityProgressService
    {
        Task<ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>> UpsertSingleAsync(UpsertSingleVisitObjectiveActivityProgressRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
    }
}