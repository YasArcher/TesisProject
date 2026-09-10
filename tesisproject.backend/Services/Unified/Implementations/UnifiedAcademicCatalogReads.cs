using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

// Read the last synchronized snapshot. External-shaped DTOs keep institutional IDs
// for existing selectors; callers use the separate local keys when persisting.
internal static class UnifiedAcademicCatalogReads
{
    internal static async Task<ServiceResult<List<AcademicTerm>>> TermsAsync(IUnifiedUnitOfWork uow, CancellationToken ct)
    {
        var rows = await uow.AcademicTerms.Query()
            .Where(x => x.ExternalPeriodId > 0 && x.StartDate != null && x.EndDate != null && x.StartDate <= x.EndDate)
            .OrderBy(x => x.StartDate).ThenBy(x => x.ExternalPeriodId).ToListAsync(ct);
        return rows.Count == 0
            ? ServiceResult<List<AcademicTerm>>.Fail(ErrorMessages.AcademicReferences.AcademicTermNotSynchronized,
                ErrorType.NotFound, ErrorCodes.AcademicReferences.AcademicTermNotSynchronized)
            : ServiceResult<List<AcademicTerm>>.Ok(rows);
    }

    internal static List<ExternalAcademicPeriodModel> Periods(IEnumerable<AcademicTerm> rows)
        => rows.Select(x => new ExternalAcademicPeriodModel
        {
            PeriodId = x.ExternalPeriodId!.Value, Name = x.Name,
            StartDate = x.StartDate!.Value, EndDate = x.EndDate!.Value
        }).ToList();

    internal static async Task<ServiceResult<List<ExternalAcademicPeriodModel>>> PeriodsAsync(IUnifiedUnitOfWork uow, CancellationToken ct)
    {
        var rows = await TermsAsync(uow, ct);
        return rows.Success ? ServiceResult<List<ExternalAcademicPeriodModel>>.Ok(Periods(rows.Data!))
            : UnifiedAcademicReferencePreparation.Relay<List<ExternalAcademicPeriodModel>, List<AcademicTerm>>(rows);
    }

    internal static async Task<ServiceResult<List<Faculty>>> FacultiesAsync(IUnifiedUnitOfWork uow, CancellationToken ct)
    {
        var rows = await uow.Faculties.Query().Where(x => x.ExternalFacultyId > 0).ToListAsync(ct);
        return rows.Any(x => x.ParentFacultyId == null)
            ? ServiceResult<List<Faculty>>.Ok(rows)
            : ServiceResult<List<Faculty>>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized,
                ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized);
    }

    // Legacy academics projection was roots plus direct children, not every descendant.
    // Preserve inactive nodes: the old provider listing did not apply DIDE IsActive.
    internal static List<ExternalFacultyDTO> FacultyRoots(IEnumerable<Faculty> rows)
    {
        var nodes = rows.ToList();
        var children = nodes.Where(x => x.ParentFacultyId.HasValue).ToLookup(x => x.ParentFacultyId!.Value);
        return nodes.Where(x => x.ParentFacultyId == null).Select(root => new ExternalFacultyDTO
        {
            FacultyId = root.ExternalFacultyId!.Value, Name = root.Name, Acronym = root.Acronym,
            Programs = children[root.FacultyId].Select(child => new ExternalProgramDTO
            {
                ProgramId = child.ExternalFacultyId!.Value, FacultyId = root.ExternalFacultyId.Value, Name = child.Name
            }).ToList()
        }).ToList();
    }
}
