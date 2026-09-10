using tesisproject.backend.Services.Unified.Contracts;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedFacultySynchronizationService
{
    Task<ServiceResult<CatalogSynchronizationResult>> SynchronizeAsync(CancellationToken ct = default);
}
