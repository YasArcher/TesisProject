using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

// Shared by scope, simple creation and the preparation stages of Identity-blocked owners.
// Resolves only existing local catalog rows. Never synchronizes, attaches, mutates or commits.
internal static class UnifiedAcademicReferencePreparation
{
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
        var faculties = new List<Faculty>();
        foreach (var id in ids)
        {
            var result = await FacultyAsync(uow, id, ct);
            if (!result.Success) return Relay<List<Faculty>, Faculty>(result);
            faculties.Add(result.Data!);
        }
        return ServiceResult<List<Faculty>>.Ok(faculties);
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
