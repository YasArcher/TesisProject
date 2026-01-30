// Frontend/Services/Implementations/VisitClientService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class VisitClientService : IVisitClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "visits";

        public VisitClientService(IApiClient api) => _api = api;

        public Task<HttpResponseWrapper<VisitListResponseDTO?>> CreateAsync(AddVisitRequestDTO request, CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/visits
            return _api.PostAsync<AddVisitRequestDTO, VisitListResponseDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //           LIST
        // =========================

        public Task<HttpResponseWrapper<List<VisitListResponseDTO>?>> GetListAsync(CancellationToken ct = default)
        {
            // GET: api/visits
            return _api.GetAsync<List<VisitListResponseDTO>>($"{_baseUrl}", ct);
        }

        public Task<HttpResponseWrapper<List<VisitListResponseDTO>?>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            // GET: api/visits/by-project/{projectId}
            return _api.GetAsync<List<VisitListResponseDTO>>($"{_baseUrl}/by-project/{projectId}", ct);
        }

        // =========================
        //          DETAIL
        // =========================

        public Task<HttpResponseWrapper<VisitDetailResponseDTO?>> GetVisitDetailAsync(int id, CancellationToken ct = default)
        {
            // GET: api/visits/{id}/detail
            return _api.GetAsync<VisitDetailResponseDTO>($"{_baseUrl}/{id}/detail", ct);
        }

        // =========================
        //          SINGLE
        // =========================

        public Task<HttpResponseWrapper<VisitListResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            // GET: api/visits/{id}
            return _api.GetAsync<VisitListResponseDTO>($"{_baseUrl}/{id}", ct);
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<VisitListResponseDTO?>> UpdateAsync(UpdateVisitRequestDTO request, CancellationToken ct = default)
        {
            if (request.VisitId <= 0)
                throw new ArgumentException("VisitId must be a positive value.", nameof(request.VisitId));

            // PUT: api/visits/{id}
            return _api.PutAsync<UpdateVisitRequestDTO, VisitListResponseDTO>(
                $"{_baseUrl}/{request.VisitId}",
                request,
                ct
            );
        }
        public Task<HttpResponseWrapper<VisitListResponseDTO?>> FinalizeAsync(
            int visitId,
            FinalizeVisitRequestDTO request,
            CancellationToken ct = default)
        {
            if (visitId <= 0)
                throw new ArgumentException("visitId must be a positive value.", nameof(visitId));

            if (request is null)
                throw new ArgumentNullException(nameof(request));


            request.VisitId = visitId;

            // PUT: api/visits/{id}/finalize
            return _api.PutAsync<FinalizeVisitRequestDTO, VisitListResponseDTO>(
                $"{_baseUrl}/{visitId}/finalize",
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
                throw new ArgumentException("id must be a positive value.", nameof(id));

            // DELETE: api/visits/{id}
            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }
        public Task<HttpResponseWrapper<List<VisitPlannedForExecutionListDTO>?>> GetPlannedForExecutionAsync(
    bool isFirstVisit,
    CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/planned-for-execution?isFirstVisit={isFirstVisit.ToString().ToLowerInvariant()}";
            return _api.GetAsync<List<VisitPlannedForExecutionListDTO>>(url, ct);
        }

        public Task<HttpResponseWrapper<NoContent>> BulkScheduleAsync(
    BulkScheduleVisitsRequestDTO request,
    CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.VisitIds is null || request.VisitIds.Count == 0)
                throw new ArgumentException("VisitIds must contain at least one id.", nameof(request.VisitIds));

            // PUT: api/visits/bulk/schedule
            return _api.PutAsync<BulkScheduleVisitsRequestDTO, NoContent>(
                $"{_baseUrl}/bulk/schedule",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<List<VisitPlannedForExecutionListDTO>?>> GetByStateAsync(
    int visitStateId,
    CancellationToken ct = default)
        {
            if (visitStateId <= 0)
                throw new ArgumentException("visitStateId must be a positive value.", nameof(visitStateId));

            // GET: api/visits/by-state/{visitStateId}
            return _api.GetAsync<List<VisitPlannedForExecutionListDTO>>(
                $"{_baseUrl}/by-state/{visitStateId}",
                ct
            );
        }
    }
}
