using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokens;

        // 👇 NUEVO: servicio que contiene la lógica de registro / APP_USER
        private readonly IAppUserService _appUserRegistration;

        public AuthService(
            UserManager<IdentityUser<int>> userManager,
            SignInManager<IdentityUser<int>> signInManager,
            ITokenService tokenService,
            IRefreshTokenService refreshTokens,
            IAppUserService appUserRegistration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokens = refreshTokens;
            _appUserRegistration = appUserRegistration;
        }

        // =============== REGISTER ===============

        public async Task<(ServiceResult<AuthResponse> Result,
                          (string token, DateTime exp)? RefreshCookie)>
           RegisterAsync(RegisterRequest dto, string? ip, CancellationToken ct)
        {
            try
            {
                // Toda la lógica de:
                // - IdentityUser (ASP local)
                // - AppUser (tabla puente)
                // vive en IAppUserRegistrationService
                var appUserId = await _appUserRegistration.EnsureAppUserAsync(dto, ct);

                // Si quisieras, aquí podrías loguear ese IdUser:
                // _logger.LogInformation("User registered with AppUserId {Id}", appUserId);

                // No generamos tokens ni refresh: NO login automático
                var resp = new AuthResponse
                {
                    TokenType = "None",
                    AccessToken = string.Empty,
                    AccessTokenExpiresAtUtc = DateTime.UtcNow,
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow
                };

                return (
                    ServiceResult<AuthResponse>.Ok(resp, $"User registered (AppUserId={appUserId}, no login performed)"),
                    null
                );
            }
            catch (DbUpdateException dbex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict),
                    null
                );
            }
            catch (Exception ex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(ex.Message, ErrorType.Unexpected),
                    null
                );
            }
        }

        // =============== LOGIN ===============

        public async Task<(ServiceResult<AuthResponse> Result,
                           (string token, DateTime exp)? RefreshCookie)>
            LoginAsync(LoginRequest dto, string? ip, CancellationToken ct)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user is null)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail("invalid_credentials", ErrorType.Validation),
                        null
                    );
                }

                var check = await _signInManager.CheckPasswordSignInAsync(
                    user,
                    dto.Password,
                    lockoutOnFailure: true);

                if (!check.Succeeded)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail("invalid_credentials", ErrorType.Validation),
                        null
                    );
                }

                var roles = await _userManager.GetRolesAsync(user);

                var (access, accessExp) = _tokenService.CreateAccessToken(user.Id, user.Email!, roles);
                var (refresh, refreshExp) = _tokenService.CreateRefreshToken();

                await _refreshTokens.CreateAsync(user.Id, refresh, refreshExp, ip, ct);
                await _refreshTokens.SaveChangesAsync(ct);

                var resp = new AuthResponse
                {
                    TokenType = "Bearer",
                    AccessToken = access,
                    AccessTokenExpiresAtUtc = accessExp,
                    RefreshTokenExpiresAtUtc = refreshExp
                };

                return (
                    ServiceResult<AuthResponse>.Ok(resp, "Login successful"),
                    (refresh, refreshExp)
                );
            }
            catch (DbUpdateException dbex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict),
                    null
                );
            }
            catch (Exception ex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(ex.Message, ErrorType.Unexpected),
                    null
                );
            }
        }

        // =============== REFRESH ===============

        public async Task<(ServiceResult<AuthResponse> Result,
                           (string token, DateTime exp)? RefreshCookie)>
            RefreshAsync(string? refreshCookie, string? ip, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshCookie))
                {
                    return (
                        ServiceResult<AuthResponse>.Fail("no_refresh_cookie", ErrorType.Validation),
                        null
                    );
                }

                var current = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
                if (current is null)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail("invalid_or_inactive_refresh_token", ErrorType.Validation),
                        null
                    );
                }

                var user = await _userManager.FindByIdAsync(current.UserId.ToString());
                if (user is null)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail("user_not_found", ErrorType.NotFound),
                        null
                    );
                }

                var (newRefresh, newRefreshExp) = _tokenService.CreateRefreshToken();

                await _refreshTokens.RevokeAsync(current, byIp: ip, replacedByToken: newRefresh, ct: ct);
                await _refreshTokens.CreateAsync(user.Id, newRefresh, newRefreshExp, ip, ct);
                await _refreshTokens.SaveChangesAsync(ct);

                var roles = await _userManager.GetRolesAsync(user);
                var (access, accessExp) = _tokenService.CreateAccessToken(user.Id, user.Email!, roles);

                var resp = new AuthResponse
                {
                    TokenType = "Bearer",
                    AccessToken = access,
                    AccessTokenExpiresAtUtc = accessExp,
                    RefreshTokenExpiresAtUtc = newRefreshExp
                };

                return (
                    ServiceResult<AuthResponse>.Ok(resp, "Token refreshed"),
                    (newRefresh, newRefreshExp)
                );
            }
            catch (DbUpdateException dbex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict),
                    null
                );
            }
            catch (Exception ex)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(ex.Message, ErrorType.Unexpected),
                    null
                );
            }
        }

        // =============== REVOKE / LOGOUT ===============

        public async Task<ServiceResult<NoContent>> RevokeAsync(
            string? refreshCookie,
            string? ip,
            CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshCookie))
                {
                    // Lo tratamos como ya “cerrado”
                    return ServiceResult<NoContent>.Ok(new NoContent(), "No refresh cookie provided.");
                }

                var active = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
                if (active is null)
                {
                    return ServiceResult<NoContent>.Ok(new NoContent(), "Refresh token already inactive.");
                }

                var again = await _refreshTokens.GetActiveAsync(active.UserId, refreshCookie, ct);
                if (again is null)
                {
                    return ServiceResult<NoContent>.Ok(new NoContent(), "Refresh token already inactive.");
                }

                await _refreshTokens.RevokeAsync(again, byIp: ip, replacedByToken: null, ct: ct);
                await _refreshTokens.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Refresh token revoked.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }
    }
}
