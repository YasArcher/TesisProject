using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Identity;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Implementations;

public class IdentityAdministrationService : IIdentityAdministrationService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly IInstitutionAuthorDirectoryService _institutionAuthors;

    public IdentityAdministrationService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        IInstitutionAuthorDirectoryService institutionAuthors)
    {
        _users = users;
        _roles = roles;
        _institutionAuthors = institutionAuthors;
    }

    public async Task<IReadOnlyCollection<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.Users
            .OrderBy(x => x.Email)
            .ToListAsync(ct);

        var items = new List<IdentityUserListItemDto>(users.Count);
        foreach (var user in users)
        {
            items.Add(await MapUserAsync(user));
        }

        return items;
    }

    public async Task<IReadOnlyCollection<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roles.Roles
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var items = new List<IdentityRoleListItemDto>(roles.Count);
        foreach (var role in roles)
        {
            var count = await _users.GetUsersInRoleAsync(role.Name!);
            items.Add(new IdentityRoleListItemDto(
                role.Id,
                role.Name ?? string.Empty,
                count.Count));
        }

        return items;
    }

    public async Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("El correo es obligatorio.");
        }

        var exists = await _users.FindByEmailAsync(email);
        if (exists is not null)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        var requestedRoles = NormalizeRoles(request.Roles);
        foreach (var roleName in requestedRoles)
        {
            await EnsureRoleExistsAsync(roleName);
        }

        if (requestedRoles.Contains(AppRoles.Author, StringComparer.OrdinalIgnoreCase))
        {
            var institutionCheck = await _institutionAuthors.ValidateAuthorAsync(email, ct);
            if (!institutionCheck.IsAllowed)
            {
                throw new InvalidOperationException(institutionCheck.Reason ?? "El usuario no está habilitado como autor institucional.");
            }
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FullName = request.FullName?.Trim()
        };

        var create = await _users.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", create.Errors.Select(x => x.Description)));
        }

        if (requestedRoles.Count > 0)
        {
            var addRoles = await _users.AddToRolesAsync(user, requestedRoles);
            if (!addRoles.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", addRoles.Errors.Select(x => x.Description)));
            }
        }

        return await MapUserAsync(user);
    }

    public async Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var roleName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new InvalidOperationException("El nombre del rol es obligatorio.");
        }

        var existing = await _roles.FindByNameAsync(roleName);
        if (existing is not null)
        {
            var existingUsers = await _users.GetUsersInRoleAsync(existing.Name!);
            return new IdentityRoleListItemDto(existing.Id, existing.Name ?? string.Empty, existingUsers.Count);
        }

        var create = await _roles.CreateAsync(new ApplicationRole { Name = roleName });
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", create.Errors.Select(x => x.Description)));
        }

        var created = await _roles.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException("No se pudo recuperar el rol creado.");

        return new IdentityRoleListItemDto(created.Id, created.Name ?? string.Empty, 0);
    }

    public async Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("No se encontró el usuario seleccionado.");

        var requestedRoles = NormalizeRoles(request.Roles);
        foreach (var roleName in requestedRoles)
        {
            await EnsureRoleExistsAsync(roleName);
        }

        var currentRoles = await _users.GetRolesAsync(user);
        var currentSet = new HashSet<string>(currentRoles, StringComparer.OrdinalIgnoreCase);
        var requestedSet = new HashSet<string>(requestedRoles, StringComparer.OrdinalIgnoreCase);

        var toRemove = currentRoles.Where(x => !requestedSet.Contains(x)).ToArray();
        if (toRemove.Length > 0)
        {
            var remove = await _users.RemoveFromRolesAsync(user, toRemove);
            if (!remove.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", remove.Errors.Select(x => x.Description)));
            }
        }

        var toAdd = requestedRoles.Where(x => !currentSet.Contains(x)).ToArray();
        if (toAdd.Length > 0)
        {
            var add = await _users.AddToRolesAsync(user, toAdd);
            if (!add.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", add.Errors.Select(x => x.Description)));
            }
        }

        return await MapUserAsync(user);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await _roles.RoleExistsAsync(roleName))
        {
            return;
        }

        var create = await _roles.CreateAsync(new ApplicationRole { Name = roleName });
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", create.Errors.Select(x => x.Description)));
        }
    }

    private async Task<IdentityUserListItemDto> MapUserAsync(ApplicationUser user)
    {
        var roles = (await _users.GetRolesAsync(user))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        return new IdentityUserListItemDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName ?? string.Empty,
            user.EmailConfirmed,
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            roles);
    }

    private static List<string> NormalizeRoles(IEnumerable<string>? roles)
        => roles?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList()
        ?? [];
}
