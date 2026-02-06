using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Entities.Auth;
using tesisproject.shared.Responses;


namespace tesisproject.backend.Services.Implementations
{
    public class AppUserService : IAppUserService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly IAppUserRepository _appUsers;
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public AppUserService(
            UserManager<IdentityUser<int>> userManager,
            IAppUserRepository appUsers,
            IUnitOfWork uow,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _appUsers = appUsers;
            _uow = uow;
            _roleManager = roleManager;
        }

        // =============== SINGLE ===============

        public async Task<ServiceResult<int>> EnsureAppUserAsync(
            RegisterRequest dto,
            CancellationToken ct = default)
        {
            try
            {
                var idUser = await EnsureSingleInternalAsync(dto, ct);
                return ServiceResult<int>.Ok(idUser, "App user ensured.");
            }
            catch (InvalidOperationException invEx)
            {
                // Errores de Identity (password, email duplicado, etc.)
                return ServiceResult<int>.Fail(invEx.Message, ErrorType.Validation);
            }
            catch (DbUpdateException dbEx)
            {
                // Conflictos con la BD (unique keys, FK, etc.)
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResult<int>.Fail(msg, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== BULK ===============

        public async Task<ServiceResult<List<int>>> EnsureAppUsersAsync(
            IEnumerable<RegisterRequest> dtos,
            CancellationToken ct = default)
        {
            try
            {
                var result = new List<int>();

                // Mantener el orden de entrada
                foreach (var dto in dtos)
                {
                    var idUser = await EnsureSingleInternalAsync(dto, ct);
                    result.Add(idUser);
                }

                return ServiceResult<List<int>>.Ok(result, "App users ensured.");
            }
            catch (InvalidOperationException invEx)
            {
                return ServiceResult<List<int>>.Fail(invEx.Message, ErrorType.Validation);
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResult<List<int>>.Fail(msg, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<int>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        /// <summary>
        /// Lógica central:
        /// - Busca o crea IdentityUser (ASP local) por Email.
        /// - Busca o crea AppUser por AspUserId / IdLocal.
        /// - Devuelve IdUser (PK de APP_USER).
        /// </summary>
        private async Task<int> EnsureSingleInternalAsync(RegisterRequest dto, CancellationToken ct)
        {
            // 1) Buscar IdentityUser por email
            var user = await _userManager.FindByEmailAsync(dto.Email);

            var userWasCreated = false;

            if (user is null)
            {
                user = new IdentityUser<int>
                {
                    Email = dto.Email,
                    UserName = dto.Username,
                    EmailConfirmed = false
                };

                var create = await _userManager.CreateAsync(user, dto.Password);
                if (!create.Succeeded)
                {
                    var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    throw new InvalidOperationException(msg);
                }

                userWasCreated = true;
            }

            // 1.1) ASIGNAR ROL (AQUÍ)
            // - Si NO quieres tocar DTO: deja roleToAssign = "technical"
            // - Si agregaste dto.Role: permite solo financial/technical
            var roleToAssign = string.IsNullOrWhiteSpace(dto.Role) ? "technical" : dto.Role.Trim();

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "technical",
        "financial",
        "coordinador",
        // NO incluyas admin/superadmin en registro público
    };

            if (!allowed.Contains(roleToAssign))
                roleToAssign = "technical"; // o lanza error si quieres ser estricto

            if (!await _roleManager.RoleExistsAsync(roleToAssign))
                throw new InvalidOperationException($"Role '{roleToAssign}' does not exist.");

            // Evita duplicar asignación
            if (!await _userManager.IsInRoleAsync(user, roleToAssign))
            {
                var addRole = await _userManager.AddToRoleAsync(user, roleToAssign);
                if (!addRole.Succeeded)
                {
                    var msg = string.Join("; ", addRole.Errors.Select(e => $"{e.Code}:{e.Description}"));

                    // rollback si el usuario se creó recién
                    if (userWasCreated)
                        await _userManager.DeleteAsync(user);

                    throw new InvalidOperationException(msg);
                }
            }

            // 2) Buscar AppUser existente por AspUserId o IdLocal
            AppUser? appUser = null;

            if (dto.AspUserId.HasValue)
                appUser = await _appUsers.GetByAspIdAsync(dto.AspUserId.Value, ct);

            if (appUser is null)
                appUser = await _appUsers.GetByLocalIdAsync(user.Id, ct);

            if (appUser is null)
            {
                appUser = new AppUser
                {
                    IdLocal = user.Id,
                    IdAsp = dto.AspUserId
                };

                await _appUsers.AddAsync(appUser, ct);
            }
            else
            {
                var modified = false;

                if (appUser.IdLocal is null)
                {
                    appUser.IdLocal = user.Id;
                    modified = true;
                }

                if (dto.AspUserId.HasValue && appUser.IdAsp is null)
                {
                    appUser.IdAsp = dto.AspUserId.Value;
                    modified = true;
                }

                if (modified)
                    _appUsers.Update(appUser);
            }

            await _uow.SaveChangesAsync(ct);
            return appUser.IdUser;
        }

    }
}
