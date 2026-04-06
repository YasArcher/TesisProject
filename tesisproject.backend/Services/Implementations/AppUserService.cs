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
        private readonly IUserRoleService _userRoles;

        public AppUserService(
            UserManager<IdentityUser<int>> userManager,
            IAppUserRepository appUsers,
            IUnitOfWork uow,
            IUserRoleService userRoles)
        {
            _userManager = userManager;
            _appUsers = appUsers;
            _uow = uow;
            _userRoles = userRoles;
        }

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
                return ServiceResult<int>.Fail(invEx.Message, ErrorType.Validation);
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResult<int>.Fail(msg, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<int>>> EnsureAppUsersAsync(
            IEnumerable<RegisterRequest> dtos,
            CancellationToken ct = default)
        {
            try
            {
                var result = new List<int>();
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

        public async Task<ServiceResult<int>> GetAppUserIdByLocalIdAsync(int localUserId, CancellationToken ct = default)
        {
            try
            {
                if (localUserId <= 0)
                {
                    return ServiceResult<int>.Fail(
                        "Invalid local user id.",
                        ErrorType.Validation
                    );
                }

                var appUser = await _appUsers.GetByLocalIdAsync(localUserId, ct);

                if (appUser is null || appUser.IdUser <= 0)
                {
                    return ServiceResult<int>.Fail(
                        "App user not found.",
                        ErrorType.NotFound
                    );
                }

                return ServiceResult<int>.Ok(appUser.IdUser, "App user resolved.");
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

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

            // 2) Roles (delegado)
            try
            {
                await _userRoles.AssignRoleAsync(user, dto.Role, ct);
            }
            catch
            {
                // Si el IdentityUser se creó en este flujo y rol falló => cleanup inmediato
                if (userWasCreated)
                    await _userManager.DeleteAsync(user);

                throw;
            }

            // 3) Buscar/crear AppUser
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

            // 4) Guardar BD propia
            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch
            {
                // Compensación: evita Identity user + rol sin AppUser
                if (userWasCreated)
                    await _userManager.DeleteAsync(user);

                throw;
            }
            return appUser.IdUser;
        }
    }
}