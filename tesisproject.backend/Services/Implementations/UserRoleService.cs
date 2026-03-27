using Microsoft.AspNetCore.Identity;
using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Implementations
{
    public class UserRoleService : IUserRoleService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        // Ajusta estos roles a tus seeds reales
        private static readonly HashSet<string> AllowedPublicRoles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "user",
                "technical",
                "financial",
                "coordinador"
            };

        private const string DefaultRole = "user";

        public UserRoleService(
            UserManager<IdentityUser<int>> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task AssignRoleAsync(
            IdentityUser<int> user,
            string? requestedRole,
            CancellationToken ct = default)
        {
            if (user is null)
                throw new InvalidOperationException("Identity user is required.");

            var roleToAssign = NormalizeRoleOrDefault(requestedRole);

            // Política: registro público SOLO roles permitidos
            if (!AllowedPublicRoles.Contains(roleToAssign))
                throw new InvalidOperationException($"Role '{roleToAssign}' is not allowed.");

            // Debe existir en Identity
            if (!await _roleManager.RoleExistsAsync(roleToAssign))
                throw new InvalidOperationException($"Role '{roleToAssign}' does not exist.");

            // Idempotente: no duplicar
            if (await _userManager.IsInRoleAsync(user, roleToAssign))
                return;

            var addRole = await _userManager.AddToRoleAsync(user, roleToAssign);
            if (!addRole.Succeeded)
            {
                var msg = string.Join("; ", addRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException(msg);
            }
        }

        private static string NormalizeRoleOrDefault(string? requestedRole)
        {
            var role = string.IsNullOrWhiteSpace(requestedRole)
                ? DefaultRole
                : requestedRole.Trim();

            return role;
        }
    }
}