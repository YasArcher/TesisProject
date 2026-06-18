using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IObjectiveActivityClientService
    {
        Task<HttpResponseWrapper<List<ObjectiveActivityListItemDTO>?>> ListByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> UpdateAsync(
            int id,
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default);

        // Nota: IApiClient.DeleteAsync retorna NoContent, aunque el controller devuelve ApiResponse<bool>.
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<bool>> SetProgressAsync(
            int objectiveActivityId,
            int visitId,
            int value,
            CancellationToken ct = default);
    }
}