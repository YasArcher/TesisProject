using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IUserRoleService
    {
        Task AssignRoleAsync(IdentityUser<int> user, string? requestedRole, CancellationToken ct = default);

        //Task RemoveRoleAsync(IdentityUser<int> user, string role, CancellationToken ct = default);

        //Task<IList<string>> GetRolesAsync(IdentityUser<int> user, CancellationToken ct = default);

        ///// <summary>
        ///// Deja EXACTAMENTE el conjunto de roles indicado (set exacto).
        ///// </summary>
        //Task SetRolesAsync(IdentityUser<int> user, IEnumerable<string> roles, CancellationToken ct = default);

        ///// <summary>
        ///// Rol único: elimina roles gestionables y asigna uno.
        ///// </summary>
        //Task ReplaceSingleRoleAsync(IdentityUser<int> user, string role, CancellationToken ct = default);
    }
}