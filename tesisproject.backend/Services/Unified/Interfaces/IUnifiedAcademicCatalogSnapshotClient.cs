using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

// Raw complete snapshots: preserve duplicates and distinguish [] from null/HTTP errors.
public interface IUnifiedAcademicCatalogSnapshotClient
{
    Task<ServiceResult<List<ExternalFacultyCareerFlatModel>>> GetFacultiesAsync(CancellationToken ct = default);
    Task<ServiceResult<List<ExternalAcademicPeriodModel>>> GetAcademicTermsAsync(CancellationToken ct = default);
}
