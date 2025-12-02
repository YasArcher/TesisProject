using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectObjectiveClientService : IProjectObjectiveClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/projectobjectives";

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
        /// GET: api/projectobjectives/by-project/{projectId}/with-activities
        /// Listado de objetivos con sus actividades.
        /// </summary>
        public Task<HttpResponseWrapper<List<ProjectObjectiveWithActivitiesDTO>?>> GetByProjectWithActivitiesAsync(
            int projectId,
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ProjectObjectiveWithActivitiesDTO>>(
                $"{_baseUrl}/by-project/{projectId}/with-activities",
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
            return _api.GetAsync<ProjectObjectiveDetailDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        // =========================
        //          CREATE
        // =========================

        /// <summary>
        /// POST: api/projectobjectives
        /// Crea un nuevo objetivo.
        /// </summary>
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

        /// <summary>
        /// PUT: api/projectobjectives/{id}
        /// Actualiza un objetivo existente.
        /// </summary>
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

        /// <summary>
        /// DELETE: api/projectobjectives/{id}
        /// Elimina un objetivo.
        /// </summary>
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
    }
}