using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalResearcherProjectClientService : IExternalResearcherProjectClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/externalresearcherprojects";

        public ExternalResearcherProjectClientService(IApiClient api)
        {
            _api = api;
        }

        public Task<HttpResponseWrapper<List<ExternalResearcherProjectListItemDTO>?>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                throw new ArgumentException("ProjectId must be a positive value.", nameof(projectId));

            // GET: api/externalresearcherprojects/by-project/{projectId}
            return _api.GetAsync<List<ExternalResearcherProjectListItemDTO>>(
                $"{_baseUrl}/by-project/{projectId}",
                ct);
        }

        public Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            // GET: api/externalresearcherprojects/{id}
            return _api.GetAsync<ExternalResearcherProjectDetailDTO>(
                $"{_baseUrl}/{id}",
                ct);
        }

        public Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/externalresearcherprojects
            return _api.PostAsync<ExternalResearcherProjectCreateRequestDTO, ExternalResearcherProjectDetailDTO>(
                _baseUrl,
                request,
                ct);
        }

        public Task<HttpResponseWrapper<ExternalResearcherProjectDetailDTO?>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            // PUT: api/externalresearcherprojects/{id}
            return _api.PutAsync<ExternalResearcherProjectUpdateRequestDTO, ExternalResearcherProjectDetailDTO>(
                $"{_baseUrl}/{request.Id}",
                request,
                ct);
        }
    }
}