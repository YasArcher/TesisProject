using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Implementations;

public interface IIdentityAdministrationService
{
    Task<IReadOnlyCollection<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default);
    Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default);
    Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default);
    Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default);
}
