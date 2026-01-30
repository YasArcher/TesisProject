using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Responses;
using static System.Net.WebRequestMethods;

namespace tesisproject.frontend.Services.Implementations
{
    public class ObjectiveActivityClientService : IObjectiveActivityClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "objectiveactivities";

        public ObjectiveActivityClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //      LIST BY OBJECTIVE
        // =========================

        public Task<HttpResponseWrapper<List<ObjectiveActivityListItemDTO>?>> ListByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default)
        {
            if (objectiveId <= 0)
                throw new ArgumentException("ObjectiveId must be a positive value.", nameof(objectiveId));

            // GET: api/objectiveactivities/by-objective/{objectiveId}
            return _api.GetAsync<List<ObjectiveActivityListItemDTO>>(
                $"{_baseUrl}/by-objective/{objectiveId}",
                ct
            );
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/objectiveactivities/{id}
            return _api.GetAsync<ObjectiveActivityDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/objectiveactivities
            return _api.PostAsync<AddObjectiveActivityRequestDTO, ObjectiveActivityDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ObjectiveActivityDetailDTO?>> UpdateAsync(
            int id,
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // Controller: request.ObjectiveActivityId = id;
            request.ObjectiveActivityId = id;

            // PUT: api/objectiveactivities/{id}
            return _api.PutAsync<UpdateObjectiveActivityRequestDTO, ObjectiveActivityDetailDTO>(
                $"{_baseUrl}/{id}",
                request,
                ct
            );
        }

        // =========================
        //          DELETE
        // =========================

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // DELETE: api/objectiveactivities/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //        SET PROGRESS
        // =========================

        public Task<HttpResponseWrapper<bool>> SetProgressAsync(
            int objectiveActivityId,
            int visitId,
            int value,
            CancellationToken ct = default)
        {
            value = Math.Min(100, Math.Max(0, value));
            var url = $"{_baseUrl}/{objectiveActivityId}/progress?visitId={visitId}&value={value}";
            return _api.PutAsync<object, bool>(url, body: new { }, ct);
        }
    }
}