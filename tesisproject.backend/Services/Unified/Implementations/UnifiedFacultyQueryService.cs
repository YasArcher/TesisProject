using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Faculty;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedFacultyQueryService(IUnifiedUnitOfWork uow) : IUnifiedFacultyQueryService
{
    // Flat adjacency preserves every depth without recursive DTOs or JSON depth limits.
    // Inactive nodes are included so filtering cannot orphan active descendants.
    public Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetHierarchyAsync(CancellationToken ct = default)
        => ReadAsync(uow.Faculties.Query(), ct);

    public Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetRootsAsync(CancellationToken ct = default)
        => ReadAsync(uow.Faculties.Query().Where(x => x.ParentFacultyId == null), ct);

    // Empty is valid for both a leaf and an unknown parent; this operation only lists children.
    public Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> GetChildrenAsync(int parentFacultyId, CancellationToken ct = default)
        => ReadAsync(uow.Faculties.Query().Where(x => x.ParentFacultyId == parentFacultyId), ct);

    private static async Task<ServiceResult<List<FacultyHierarchyNodeDTO>>> ReadAsync(IQueryable<Faculty> query, CancellationToken ct)
    {
        try
        {
            var nodes = await query.OrderBy(x => x.Name).ThenBy(x => x.FacultyId)
                .Select(x => new FacultyHierarchyNodeDTO
                {
                    FacultyId = x.FacultyId, ExternalFacultyId = x.ExternalFacultyId,
                    ParentFacultyId = x.ParentFacultyId, Name = x.Name, Acronym = x.Acronym,
                    IsActive = x.IsActive, LastSyncedAt = x.LastSyncedAt
                }).ToListAsync(ct);
            return ServiceResult<List<FacultyHierarchyNodeDTO>>.Ok(nodes);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            return ServiceResult<List<FacultyHierarchyNodeDTO>>.Fail(ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
        }
    }
}
