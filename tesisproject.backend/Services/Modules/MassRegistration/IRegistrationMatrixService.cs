using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IRegistrationMatrixService
    {
        Task<List<RegistrationMatrixSummaryDto>> GetMatricesAsync(int take = 50, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> GetMatrixAsync(int matrixId, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto> CreateMatrixAsync(CreateRegistrationMatrixRequest request, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> UpdateMatrixAsync(int matrixId, UpdateRegistrationMatrixRequest request, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> AddColumnsAsync(int matrixId, AddRegistrationMatrixColumnsRequest request, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> UpdateColumnOrderAsync(int matrixId, int columnId, UpdateRegistrationMatrixColumnOrderRequest request, CancellationToken ct = default);
        Task<bool> RemoveColumnAsync(int matrixId, int columnId, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> AddRowAsync(int matrixId, CancellationToken ct = default);
        Task<RegistrationMatrixDetailDto?> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default);
        Task<bool> DeleteRowAsync(int matrixId, int rowId, CancellationToken ct = default);
        Task<RegistrationMatrixSubmissionResultDto> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? userId, CancellationToken ct = default);
    }
}
