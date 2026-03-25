using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.shared.Abstractions.Auth;
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

    public async Task<(string Email, string FullName, string[] Roles)> MeAsync(ClaimsPrincipal userClaims)
    {
        var email = userClaims.FindFirstValue(ClaimTypes.Email) ?? "";
        var user = await _users.FindByEmailAsync(email);
        if (user == null) throw new UnauthorizedAccessException();

        var roles = await _users.GetRolesAsync(user);
        return (user.Email!, user.FullName ?? "", roles.ToArray());
    }
}