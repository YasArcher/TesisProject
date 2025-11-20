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
        private readonly string _baseUrl = "api/visits";

        public VisitClientService(IApiClient api) => _api = api;

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
    }
}
