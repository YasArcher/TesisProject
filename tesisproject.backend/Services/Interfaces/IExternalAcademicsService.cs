using tesisproject.shared.DTOs.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalAcademicsService
    {
        Task<ServiceResult<List<ExternalFacultyDTO>>> GetFacultiesWithProgramsAsync(CancellationToken ct = default);
        Task<ServiceResult<ExternalFacultyDTO>> GetFacultyByIdAsync(int facultyId, CancellationToken ct = default);
        Task<ServiceResult<List<ExternalProgramDTO>>> GetProgramsByFacultyIdAsync(int facultyId, CancellationToken ct = default);
        Task<ServiceResult<ExternalProgramDTO>> GetProgramByIdAsync(int programId, CancellationToken ct = default);
    }
}