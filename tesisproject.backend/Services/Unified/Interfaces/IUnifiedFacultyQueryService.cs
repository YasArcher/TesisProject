using tesisproject.shared.DTOs.Faculty;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedFacultyQueryService
{
    Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetHierarchyAsync(CancellationToken ct = default);
    Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetRootsAsync(CancellationToken ct = default);
    Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetChildrenAsync(int parentFacultyId, CancellationToken ct = default);
}
