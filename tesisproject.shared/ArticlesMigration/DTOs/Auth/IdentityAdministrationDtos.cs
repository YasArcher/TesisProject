namespace tesisproject.shared.DTOs.Auth;

public record IdentityUserListItemDto(
    string UserId,
    string Email,
    string FullName,
    bool EmailConfirmed,
    bool IsLockedOut,
    string[] Roles);

public record IdentityRoleListItemDto(
    string RoleId,
    string Name,
    int UserCount);

public record CreateIdentityUserRequest(
    string Email,
    string Password,
    string FullName,
    string[] Roles);

public record CreateIdentityRoleRequest(string Name);

public record UpdateIdentityUserRolesRequest(string[] Roles);
