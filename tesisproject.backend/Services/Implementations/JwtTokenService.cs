using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using tesisproject.backend.Services.Interfaces;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _cfg;
    public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

    public (string token, DateTime expiresAtUtc) CreateAccessToken(int userId, string? email, IList<string> roles)
    {
        var issuer = _cfg["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var audience = _cfg["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
        var keyRaw = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

        if (keyRaw.Length < 32) // ~256 bits
            throw new InvalidOperationException("Jwt:Key must be at least 32 chars (≈256 bits).");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyRaw));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var minutes = int.TryParse(_cfg["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
        var nowUtc = DateTime.UtcNow;
        var expires = nowUtc.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, userId.ToString()),

            // para Blazor/AuthorizeView y políticas:
            new("name", email ?? $"user:{userId}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, ToUnixSeconds(nowUtc), ClaimValueTypes.Integer64),
        };

        // Emitimos ambos tipos de role-claims por compatibilidad
        foreach (var r in roles ?? Array.Empty<string>())
        {
            claims.Add(new Claim("role", r));
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string token, DateTime expiresAtUtc) CreateRefreshToken()
    {
        var days = int.TryParse(_cfg["Jwt:RefreshTokenDays"], out var d) ? d : 30;
        var expires = DateTime.UtcNow.AddDays(days);

        // 64 bytes de entropía
        var buffer = new byte[64];
        RandomNumberGenerator.Fill(buffer);

        // Base64URL requiere byte[]
        var rawUrl = Base64UrlEncoder.Encode(buffer);

        return (rawUrl, expires);
    }

    private static string ToUnixSeconds(DateTime utc)
        => ((long)Math.Round((utc - DateTime.UnixEpoch).TotalSeconds)).ToString();
}
