using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IVisitObjectiveActivityProgressClientService
    {
        Task<HttpResponseWrapper<VisitObjectiveActivityProgressSingleResponseDTO?>> UpsertSingleAsync(
            UpsertSingleVisitObjectiveActivityProgressRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}