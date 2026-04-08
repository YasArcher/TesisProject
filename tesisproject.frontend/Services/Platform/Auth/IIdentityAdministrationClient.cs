using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Platform.Auth;

public interface IIdentityAdministrationClient
{
    Task<IReadOnlyList<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default);
    Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default);
    Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default);
    Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default);
}
