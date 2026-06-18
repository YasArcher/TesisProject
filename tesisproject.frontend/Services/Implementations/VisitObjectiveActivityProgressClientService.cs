using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class VisitObjectiveActivityProgressClientService : IVisitObjectiveActivityProgressClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "visitobjectiveactivityprogresses";

        public VisitObjectiveActivityProgressClientService(IApiClient api) => _api = api;

        public Task<HttpResponseWrapper<VisitObjectiveActivityProgressSingleResponseDTO?>> UpsertSingleAsync(
            UpsertSingleVisitObjectiveActivityProgressRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.VisitId <= 0)
                throw new ArgumentException("VisitId must be a positive value.", nameof(request.VisitId));

            if (request.ObjectiveActivityId <= 0)
                throw new ArgumentException("ObjectiveActivityId must be a positive value.", nameof(request.ObjectiveActivityId));

            if (request.ProgressPercentage < 0 || request.ProgressPercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(request.ProgressPercentage), "ProgressPercentage must be between 0 and 100.");

            // PUT: api/visitobjectiveactivityprogresses
            return _api.PutAsync<UpsertSingleVisitObjectiveActivityProgressRequestDTO, VisitObjectiveActivityProgressSingleResponseDTO>(
                _baseUrl,
                request,
                ct
            );
        }
        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("id must be a positive value.", nameof(id));

            // DELETE: api/visitobjectiveactivityprogresses/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}