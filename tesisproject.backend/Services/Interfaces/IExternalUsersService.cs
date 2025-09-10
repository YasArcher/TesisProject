using tesisproject.shared.DTOs.External;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalUsersService
    {
        Task<ExternalUserDTO?> GetByIdAsync(int userId, CancellationToken ct = default);
        Task<List<ExternalUserDTO>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
    }
}
