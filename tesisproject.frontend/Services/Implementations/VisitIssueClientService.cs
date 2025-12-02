using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class VisitIssueClientService : IVisitIssueClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/VisitIssues";

        public VisitIssueClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<VisitIssueResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/visit-issues/{id}
            return _api.GetAsync<VisitIssueResponseDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //      LIST BY VISIT
        // =========================

        public Task<HttpResponseWrapper<List<VisitIssueResponseDTO>?>> ListByVisitAsync(
            int visitId,
            CancellationToken ct = default)
        {
            if (visitId <= 0)
                throw new ArgumentException("VisitId must be a positive value.", nameof(visitId));

            // GET: api/visit-issues/by-visit/{visitId}
            return _api.GetAsync<List<VisitIssueResponseDTO>>(
                $"{_baseUrl}/by-visit/{visitId}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<VisitIssueResponseDTO?>> CreateAsync(
            VisitIssueCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/visit-issues
            return _api.PostAsync<VisitIssueCreateRequestDTO, VisitIssueResponseDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<VisitIssueResponseDTO?>> UpdateAsync(
            int id,
            VisitIssueUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // PATCH: api/visit-issues/{id}
            return _api.PatchAsync<VisitIssueUpdateRequestDTO, VisitIssueResponseDTO>(
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

            // DELETE: api/visit-issues/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
    }
}