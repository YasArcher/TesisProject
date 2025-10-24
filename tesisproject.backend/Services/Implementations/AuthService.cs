using Microsoft.AspNetCore.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokens;

        public AuthService(
            UserManager<IdentityUser<int>> userManager,
            SignInManager<IdentityUser<int>> signInManager,
            ITokenService tokenService,
            IRefreshTokenService refreshTokens)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokens = refreshTokens;
        }

        public async Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)>
            RegisterAsync(RegisterRequest dto, string? ip, CancellationToken ct)
        {
            // evita duplicados
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                return (null, StatusCodes.Status400BadRequest, "email_already_exists", null);

            var user = new IdentityUser<int>
            {
                Email = dto.Email,
                UserName = dto.Username,
                EmailConfirmed = false
            };

            var create = await _userManager.CreateAsync(user, dto.Password);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                return (null, StatusCodes.Status400BadRequest, msg, null);
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

            return (resp, StatusCodes.Status200OK, null, (refresh, refreshExp));
        }

        public async Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)>
            LoginAsync(LoginRequest dto, string? ip, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return (null, StatusCodes.Status401Unauthorized, "invalid_credentials", null);

            var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!check.Succeeded)
                return (null, StatusCodes.Status401Unauthorized, "invalid_credentials", null);

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

            return (resp, StatusCodes.Status200OK, null, (refresh, refreshExp));
        }

        public async Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)>
            RefreshAsync(string? refreshCookie, string? ip, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(refreshCookie))
                return (null, StatusCodes.Status401Unauthorized, "no_refresh_cookie", null);

            // Solo podemos validar si el token está ACTIVO con el método disponible
            var current = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
            if (current is null)
                return (null, StatusCodes.Status401Unauthorized, "invalid_or_inactive_refresh_token", null);

            var user = await _userManager.FindByIdAsync(current.UserId.ToString());
            if (user is null)
                return (null, StatusCodes.Status401Unauthorized, "user_not_found", null);

            // Rotación básica: revoca el actual y crea uno nuevo
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

            return (resp, StatusCodes.Status200OK, null, (newRefresh, newRefreshExp));
        }

        public async Task RevokeAsync(string? refreshCookie, string? ip, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(refreshCookie))
                return;

            // Necesitamos el userId para GetActiveAsync; lo resolvemos buscando primero por token activo
            var active = await _refreshTokens.GetActiveByTokenAsync(refreshCookie, ct);
            if (active is null)
                return;

            var again = await _refreshTokens.GetActiveAsync(active.UserId, refreshCookie, ct);
            if (again is null)
                return;

            await _refreshTokens.RevokeAsync(again, byIp: ip, replacedByToken: null, ct: ct);
            await _refreshTokens.SaveChangesAsync(ct);
        }
    }
}