using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.frontend.Services.Interfaces;

public interface IRegistrationMatrixClient
{
    Task<HttpResponseWrapper<List<RegistrationMatrixSummaryDto>?>> GetMatricesAsync(int take = 50, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> GetMatrixAsync(int matrixId, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> AddRowAsync(int matrixId, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixDetailDto?>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixDeleteResultDto?>> DeleteMatrixAsync(int matrixId, CancellationToken ct = default);
    Task<HttpResponseWrapper<RegistrationMatrixSubmissionResultDto?>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, CancellationToken ct = default);
}
