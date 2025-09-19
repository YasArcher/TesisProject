using tesisproject.shared.DTOs.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalUsersService
    {
        Task<ServiceResult<ExternalUserDTO>> GetByIdAsync(int userId, CancellationToken ct = default);
        Task<ServiceResult<List<ExternalUserDTO>>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
        Task<ServiceResult<List<ExternalUserDTO>>> GetAllAsync(CancellationToken ct = default);
    }
}
