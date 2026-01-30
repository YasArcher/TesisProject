using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.External;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalAcademicsClientService : IExternalAcademicsClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "external/externalacademics";

        public ExternalAcademicsClientService(IApiClient api)
            => _api = api;

        /// <inheritdoc />
        public Task<HttpResponseWrapper<List<ExternalFacultyDTO>?>> GetFacultiesWithProgramsAsync(
            CancellationToken ct = default)
        {
            // GET: api/external/ExternalAcademics/faculties
            return _api.GetAsync<List<ExternalFacultyDTO>>(
                $"{_baseUrl}/faculties",
                ct
            );
        }
    }
}