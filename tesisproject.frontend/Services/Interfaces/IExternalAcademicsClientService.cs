using tesisproject.shared.DTOs.External;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalAcademicsClientService
    {
        /// <summary>
        /// Gets faculties with their programs from the external academic API.
        /// Maps to GET: api/external/ExternalAcademics/faculties
        /// </summary>
        Task<HttpResponseWrapper<List<ExternalFacultyDTO>?>> GetFacultiesWithProgramsAsync(
            CancellationToken ct = default);
    }
}
