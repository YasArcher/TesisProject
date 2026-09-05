using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces;

public interface IRegistrationMatrixService
{
    Task<ServiceResult<List<RegistrationMatrixSummaryDto>>> GetMatricesAsync(int take, string? ownerUserId, bool includeAll, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixDetailDto>> GetMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixDetailDto>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixDetailDto>> AddRowAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixDetailDto>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixDeleteResultDto>> DeleteMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default);
    Task<ServiceResult<RegistrationMatrixSubmissionResultDto>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default);
}
