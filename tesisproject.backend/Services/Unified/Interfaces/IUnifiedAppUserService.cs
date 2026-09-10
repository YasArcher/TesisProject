using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedAppUserService
{
    // IdLocal (Identity) -> IdUser (business AppUser), by repository lookup only.
    Task<ServiceResult<int>> GetAppUserIdByLocalIdAsync(int localUserId, CancellationToken ct = default);
    Task<ServiceResult<int>> EnsureAppUserAsync(tesisproject.shared.DTOs.Auth.RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<List<int>>> EnsureAppUsersAsync(IEnumerable<tesisproject.shared.DTOs.Auth.RegisterRequest> requests, CancellationToken ct = default);
}
