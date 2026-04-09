using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private const string NoneTokenType = "None";
        private const string BearerTokenType = "Bearer";
        private const string RegisterOkMessageTemplate = "User registered (AppUserId={0}, no login performed)";
        private const string LoginSuccessfulMessage = "Login successful";
        private const string TokenRefreshedMessage = "Token refreshed";
        private const string NoRefreshCookieProvidedMessage = "No refresh cookie provided.";
        private const string RefreshTokenAlreadyInactiveMessage = "Refresh token already inactive.";
        private const string RefreshTokenRevokedMessage = "Refresh token revoked.";

        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokens;
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

        public async Task<(ServiceResult<AuthResponse> Result,
                          (string token, DateTime exp)? RefreshCookie)>
           RegisterAsync(RegisterRequest dto, string? ip, CancellationToken ct)
        {
            try
            {
                var appUserResult = await _appUserRegistration.EnsureAppUserAsync(dto, ct);

                if (!appUserResult.Success)
                {
                    return (RelayFailure<AuthResponse, int>(appUserResult), null);
                }

                var appUserId = appUserResult.Data;

                if (appUserId <= 0)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Common.UnexpectedError,
                            ErrorType.Unexpected,
                            ErrorCodes.Common.UnexpectedError),
                        null
                    );
                }

                var resp = new AuthResponse
                {
                    TokenType = NoneTokenType,
                    AccessToken = string.Empty,
                    AccessTokenExpiresAtUtc = DateTime.UtcNow,
                    RefreshTokenExpiresAtUtc = DateTime.UtcNow
                };

                return (
                    ServiceResult<AuthResponse>.Ok(
                        resp,
                        string.Format(RegisterOkMessageTemplate, appUserId)),
                    null
                );
            }
            catch (DbUpdateException)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.PersistenceConflict,
                        ErrorType.Conflict,
                        ErrorCodes.Common.PersistenceConflict),
                    null
                );
            }
            catch (Exception)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.UnexpectedError,
                        ErrorType.Unexpected,
                        ErrorCodes.Common.UnexpectedError),
                    null
                );
            }
        }

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
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Auth.InvalidCredentials,
                            ErrorType.Unauthorized,
                            ErrorCodes.Auth.InvalidCredentials),
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
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Auth.InvalidCredentials,
                            ErrorType.Unauthorized,
                            ErrorCodes.Auth.InvalidCredentials),
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
                    TokenType = BearerTokenType,
                    AccessToken = access,
                    AccessTokenExpiresAtUtc = accessExp,
                    RefreshTokenExpiresAtUtc = refreshExp
                };

                return (
                    ServiceResult<AuthResponse>.Ok(resp, LoginSuccessfulMessage),
                    (refresh, refreshExp)
                );
            }
            catch (DbUpdateException)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.PersistenceConflict,
                        ErrorType.Conflict,
                        ErrorCodes.Common.PersistenceConflict),
                    null
                );
            }
            catch (Exception)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.UnexpectedError,
                        ErrorType.Unexpected,
                        ErrorCodes.Common.UnexpectedError),
                    null
                );
            }
        }

        public async Task<(ServiceResult<AuthResponse> Result,
                           (string token, DateTime exp)? RefreshCookie)>
            RefreshAsync(string? refreshCookie, string? ip, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshCookie))
                {
                    return (
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Auth.NoRefreshCookie,
                            ErrorType.Unauthorized,
                            ErrorCodes.Auth.NoRefreshCookie),
                        null
                    );
                }

                var current = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
                if (current is null)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Auth.InvalidOrInactiveRefreshToken,
                            ErrorType.Unauthorized,
                            ErrorCodes.Auth.InvalidOrInactiveRefreshToken),
                        null
                    );
                }

                var user = await _userManager.FindByIdAsync(current.UserId.ToString());
                if (user is null)
                {
                    return (
                        ServiceResult<AuthResponse>.Fail(
                            ErrorMessages.Auth.UserNotFound,
                            ErrorType.NotFound,
                            ErrorCodes.Auth.UserNotFound),
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
                    TokenType = BearerTokenType,
                    AccessToken = access,
                    AccessTokenExpiresAtUtc = accessExp,
                    RefreshTokenExpiresAtUtc = newRefreshExp
                };

                return (
                    ServiceResult<AuthResponse>.Ok(resp, TokenRefreshedMessage),
                    (newRefresh, newRefreshExp)
                );
            }
            catch (DbUpdateException)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.PersistenceConflict,
                        ErrorType.Conflict,
                        ErrorCodes.Common.PersistenceConflict),
                    null
                );
            }
            catch (Exception)
            {
                return (
                    ServiceResult<AuthResponse>.Fail(
                        ErrorMessages.Common.UnexpectedError,
                        ErrorType.Unexpected,
                        ErrorCodes.Common.UnexpectedError),
                    null
                );
            }
        }

        public async Task<ServiceResult<NoContent>> RevokeAsync(
            string? refreshCookie,
            string? ip,
            CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshCookie))
                {
                    return ServiceResult<NoContent>.Ok(new NoContent(), NoRefreshCookieProvidedMessage);
                }

                var active = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
                if (active is null)
                {
                    return ServiceResult<NoContent>.Ok(new NoContent(), RefreshTokenAlreadyInactiveMessage);
                }

                var again = await _refreshTokens.GetActiveAsync(active.UserId, refreshCookie, ct);
                if (again is null)
                {
                    return ServiceResult<NoContent>.Ok(new NoContent(), RefreshTokenAlreadyInactiveMessage);
                }

                await _refreshTokens.RevokeAsync(again, byIp: ip, replacedByToken: null, ct: ct);
                await _refreshTokens.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), RefreshTokenRevokedMessage);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.Common.PersistenceConflict,
                    ErrorType.Conflict,
                    ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.Common.UnexpectedError,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.UnexpectedError);
            }
        }

        private static ServiceResult<TTarget> RelayFailure<TTarget, TSource>(ServiceResult<TSource> source)
        {
            var error = source.Error == ErrorType.None
                ? ErrorType.Unexpected
                : source.Error;

            return ServiceResult<TTarget>.Fail(
                source.Message ?? ErrorMessages.Common.UnexpectedError,
                error,
                source.ErrorCode ?? (error == ErrorType.Unexpected ? ErrorCodes.Common.UnexpectedError : null),
                source.ValidationErrors);
        }
    }
}