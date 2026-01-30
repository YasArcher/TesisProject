using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectObjectiveClientService : IProjectObjectiveClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "projectobjectives";

        public ProjectObjectiveClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //           LISTS
        // =========================

        /// <summary>
        /// GET: api/projectobjectives/by-project/{projectId}
        /// Listado simple (sin actividades).
        /// </summary>
        public Task<HttpResponseWrapper<List<ProjectObjectiveListItemDTO>?>> GetByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ProjectObjectiveListItemDTO>>(
                $"{_baseUrl}/by-project/{projectId}",
                ct
            );
        }

        /// <summary>
        /// GET: api/projectobjectives/by-visit/{projectId}/{visitId}/with-activities
        /// Objetivos con actividades + progreso PARA ESA VISITA.
        /// </summary>
        public Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByVisitWithActivitiesAsync(
            int projectId,
            int visitId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                throw new ArgumentException("projectId must be a positive value.", nameof(projectId));

            if (visitId <= 0)
                throw new ArgumentException("visitId must be a positive value.", nameof(visitId));

            return _api.GetAsync<List<ProjectObjectiveWithActivitiesDTO>>(
                $"{_baseUrl}/by-visit/{projectId}/{visitId}/with-activities",
                ct
            );
        }

        // =========================
        //           DETAIL
        // =========================

        /// <summary>
        /// GET: api/projectobjectives/{id}
        /// Detalle de un objetivo (sin actividades).
        /// </summary>
        public Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(id));

            return _api.GetAsync<ProjectObjectiveDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        public Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> CreateAsync(
            AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return _api.PostAsync<AddProjectObjectiveRequestDTO, ProjectObjectiveDetailDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        // =========================
        //          UPDATE
        // =========================

        public Task<HttpResponseWrapper<ProjectObjectiveDetailDTO?>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.Id <= 0)
                throw new ArgumentException("Id must be a positive value.", nameof(request.Id));

            return _api.PutAsync<UpdateProjectObjectiveRequestDTO, ProjectObjectiveDetailDTO>(
                $"{_baseUrl}/{request.Id}",
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

            return _api.DeleteAsync(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        public Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByProjectWithActivitiesAsync(
    int projectId,
    CancellationToken ct = default)
        {
            if (projectId <= 0)
                throw new ArgumentException("projectId must be a positive value.", nameof(projectId));

            return _api.GetAsync<List<ProjectObjectiveWithActivitiesDTO>>(
                $"{_baseUrl}/by-project/{projectId}/with-activities",
                ct
            );
        }
    }
}