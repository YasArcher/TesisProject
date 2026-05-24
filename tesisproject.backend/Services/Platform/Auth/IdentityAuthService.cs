using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Implementations;

public class IdentityAuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _jwt;

    public IdentityAuthService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ITokenService tokens,
        IOptions<JwtOptions> jwt)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _jwt = jwt.Value;
    }

    public async Task<LoguinResponse> RegisterAsync(RegisterUserRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is not null)
            throw new InvalidOperationException("El usuario ya existe.");

        var newUser = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName
        };

        var result = await _users.CreateAsync(newUser, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));

        if (!await _roles.RoleExistsAsync(request.Role!))
            await _roles.CreateAsync(new ApplicationRole { Name = request.Role });

        await _users.AddToRoleAsync(newUser, request.Role!);

        var roles = await _users.GetRolesAsync(newUser);
        return _tokens.CreateToken(newUser, roles, _jwt);
    }

    public async Task<LoguinResponse> LoginAsync(LoguinRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        var roles = await _users.GetRolesAsync(user);
        return _tokens.CreateToken(user, roles, _jwt);
    }

    public async Task<AuthMeResponse> MeAsync(ClaimsPrincipal userClaims)
    {
        var user = await ResolveCurrentUserAsync(userClaims)
            ?? throw new UnauthorizedAccessException();

        var roles = await _users.GetRolesAsync(user);
        return MapCurrentUser(user, roles);
    }

    public async Task<AuthMeResponse> AcceptTermsAsync(ClaimsPrincipal userClaims, AcceptTermsRequest request, CancellationToken ct = default)
    {
        var user = await ResolveCurrentUserAsync(userClaims)
            ?? throw new UnauthorizedAccessException();

        user.TermsAcceptedAt = DateTime.UtcNow;
        user.TermsVersion = string.IsNullOrWhiteSpace(request.TermsVersion)
            ? "2026-04"
            : request.TermsVersion.Trim();

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
        }

        var roles = await _users.GetRolesAsync(user);
        return MapCurrentUser(user, roles);
    }

    private async Task<ApplicationUser?> ResolveCurrentUserAsync(ClaimsPrincipal userClaims)
    {
        if (userClaims?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var byId = await _users.FindByIdAsync(userId);
            if (byId is not null)
            {
                return byId;
            }
        }

        var email = userClaims.FindFirstValue(ClaimTypes.Email)
            ?? userClaims.FindFirstValue(ClaimTypes.Name);

        return string.IsNullOrWhiteSpace(email)
            ? null
            : await _users.FindByEmailAsync(email);
    }

    private static AuthMeResponse MapCurrentUser(ApplicationUser user, IEnumerable<string> roles)
    {
        return new AuthMeResponse
        {
            Email = user.Email ?? user.UserName ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? user.Email ?? string.Empty : user.FullName,
            Roles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            HasAcceptedTerms = user.TermsAcceptedAt.HasValue,
            TermsAcceptedAt = user.TermsAcceptedAt,
            TermsVersion = user.TermsVersion
        };
    }
}
