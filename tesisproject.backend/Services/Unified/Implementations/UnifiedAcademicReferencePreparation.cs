using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

// Shared by scope, simple creation and the preparation stages of Identity-blocked owners.
// Resolves only existing local catalog rows. Never synchronizes, attaches, mutates or commits.
internal static class UnifiedAcademicReferencePreparation
{
    // Career selection remains live, but its academic parent belongs to the local snapshot.
    // Read once to support any depth and reflect moves without trusting a stale directory parent.
    internal static async Task<ServiceResult<Faculty>> RootForCareerAsync(IUnifiedUnitOfWork uow, int externalNodeId, CancellationToken ct)
    {
        if (externalNodeId <= 0) return Invalid<Faculty>(nameof(externalNodeId));
        var rows = await uow.Faculties.Query().ToListAsync(ct);
        var node = rows.SingleOrDefault(x => x.ExternalFacultyId == externalNodeId);
        var byLocalId = rows.ToDictionary(x => x.FacultyId);
        var seen = new HashSet<int>();
        while (node?.ParentFacultyId is int parentId)
        {
            if (!seen.Add(node.FacultyId) || !byLocalId.TryGetValue(parentId, out node))
                return ServiceResult<Faculty>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized,
                    ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized);
        }
        return node is null
            ? ServiceResult<Faculty>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized,
                ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized)
            : ServiceResult<Faculty>.Ok(node);
    }

    internal static async Task<ServiceResult<Faculty>> FacultyAsync(IUnifiedUnitOfWork uow, int externalFacultyId, CancellationToken ct)
    {
        if (externalFacultyId <= 0) return Invalid<Faculty>(nameof(externalFacultyId));
        var faculty = await uow.Faculties.GetByExternalFacultyIdAsync(externalFacultyId, ct);
        return faculty is null
            ? ServiceResult<Faculty>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized, ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized)
            : ServiceResult<Faculty>.Ok(faculty);
    }

    internal static async Task<ServiceResult<List<Faculty>>> FacultiesAsync(IUnifiedUnitOfWork uow, IEnumerable<int>? externalFacultyIds, CancellationToken ct)
    {
        var ids = (externalFacultyIds ?? []).Distinct().ToList();
        if (ids.Any(id => id <= 0)) return Invalid<List<Faculty>>(nameof(externalFacultyIds));
        if (ids.Count == 0) return ServiceResult<List<Faculty>>.Ok([]);
        var rows = await uow.Faculties.Query().Where(x => x.ExternalFacultyId.HasValue && ids.Contains(x.ExternalFacultyId.Value)).ToListAsync(ct);
        var byExternalId = rows.ToDictionary(x => x.ExternalFacultyId!.Value);
        if (ids.Any(id => !byExternalId.ContainsKey(id)))
            return ServiceResult<List<Faculty>>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized,
                ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized);
        return ServiceResult<List<Faculty>>.Ok(ids.Select(id => byExternalId[id]).ToList());
    }

    internal static async Task<ServiceResult<AcademicTerm>> AcademicTermAsync(IUnifiedUnitOfWork uow, int externalPeriodId, CancellationToken ct)
    {
        if (externalPeriodId <= 0) return Invalid<AcademicTerm>(nameof(externalPeriodId));
        var term = await uow.AcademicTerms.GetByExternalPeriodIdAsync(externalPeriodId, ct);
        return term is null
            ? ServiceResult<AcademicTerm>.Fail(ErrorMessages.AcademicReferences.AcademicTermNotSynchronized, ErrorType.NotFound, ErrorCodes.AcademicReferences.AcademicTermNotSynchronized)
            : ServiceResult<AcademicTerm>.Ok(term);
    }

    internal static ServiceResult<T> Invalid<T>(string field) => ServiceResult<T>.Fail(
        ErrorMessages.Common.InvalidId, ErrorType.Validation, ErrorCodes.Common.InvalidId,
        new Dictionary<string, string[]> { [field] = [ErrorMessages.Common.InvalidId] });

    internal static ServiceResult<T> Relay<T, TSource>(ServiceResult<TSource> result) =>
        ServiceResult<T>.Fail(result.Message!, result.Error, result.ErrorCode, result.ValidationErrors);
}
