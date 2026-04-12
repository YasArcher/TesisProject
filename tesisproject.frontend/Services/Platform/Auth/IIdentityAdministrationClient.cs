using tesisproject.shared.DTOs.Auth;
using tesisproject.frontend.Services.Platform.Api;

namespace tesisproject.frontend.Services.Platform.Auth;

public interface IIdentityAdministrationClient
{
    Task<IReadOnlyList<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default);
    Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default);
    Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default);
    Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default);

    Task<HttpResponseWrapper<IReadOnlyList<IdentityUserListItemDto>?>> GetUsersResultAsync(CancellationToken ct = default);
    Task<HttpResponseWrapper<IReadOnlyList<IdentityRoleListItemDto>?>> GetRolesResultAsync(CancellationToken ct = default);
    Task<HttpResponseWrapper<IdentityUserListItemDto?>> CreateUserResultAsync(CreateIdentityUserRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<IdentityRoleListItemDto?>> CreateRoleResultAsync(CreateIdentityRoleRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<IdentityUserListItemDto?>> UpdateUserRolesResultAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default);
}
