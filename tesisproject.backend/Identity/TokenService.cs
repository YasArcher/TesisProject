using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Identity;
public interface ITokenService
{
    LoguinResponse CreateToken(ApplicationUser user, IEnumerable<string> roles, JwtOptions options);
}

public class TokenService : ITokenService
{
    public LoguinResponse CreateToken(ApplicationUser user, IEnumerable<string> roles, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Key))
            throw new ArgumentException("JWT Key no configurada.");

        var nowUtc = DateTime.UtcNow;
        var expires = nowUtc.AddMinutes(options.ExpirationMinutes);

        // ===== Claims =====
        var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

        // Agregamos los roles
        foreach (var role in roles.Distinct())
            claims.Add(new Claim(ClaimTypes.Role, role));

        // ===== Clave y firma =====
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ===== Generar el token JWT =====
        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expires,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        // ===== Crear y devolver el LoguinResponse =====
        return new LoguinResponse(
            AccessToken: token,
            ExpiresAt: expires,
            Email: user.Email ?? string.Empty,
            Roles: roles.Distinct().ToArray()
        );
    }
}

