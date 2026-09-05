using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations;

public sealed class DisabledRegistrationMatrixService : IRegistrationMatrixService
{
    public Task<ServiceResult<List<RegistrationMatrixSummaryDto>>> GetMatricesAsync(int take, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<List<RegistrationMatrixSummaryDto>>();
    public Task<ServiceResult<RegistrationMatrixDetailDto>> GetMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<RegistrationMatrixDetailDto>();
    public Task<ServiceResult<RegistrationMatrixDetailDto>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default) => Disabled<RegistrationMatrixDetailDto>();
    public Task<ServiceResult<RegistrationMatrixDetailDto>> AddRowAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<RegistrationMatrixDetailDto>();
    public Task<ServiceResult<RegistrationMatrixDetailDto>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<RegistrationMatrixDetailDto>();
    public Task<ServiceResult<RegistrationMatrixDeleteResultDto>> DeleteMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<RegistrationMatrixDeleteResultDto>();
    public Task<ServiceResult<RegistrationMatrixSubmissionResultDto>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default) => Disabled<RegistrationMatrixSubmissionResultDto>();

    private static Task<ServiceResult<T>> Disabled<T>()
        => Task.FromResult(ServiceResult<T>.Fail("El modulo de matriz de articulos todavia no esta habilitado en este entorno.", ErrorType.Unexpected, "ARTICLES_MATRIX_DISABLED"));
}
