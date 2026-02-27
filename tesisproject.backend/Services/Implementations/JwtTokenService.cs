using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using tesisproject.backend.Services.Interfaces;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _cfg;

    // ======= Config keys (strings exactos) =======
    private const string JwtIssuerKey = "Jwt:Issuer";
    private const string JwtAudienceKey = "Jwt:Audience";
    private const string JwtKeyKey = "Jwt:Key";
    private const string JwtAccessTokenMinutesKey = "Jwt:AccessTokenMinutes";
    private const string JwtRefreshTokenDaysKey = "Jwt:RefreshTokenDays";

    // ======= Exception messages (texto exacto) =======
    private const string JwtIssuerMissingMessage = "Jwt:Issuer missing";
    private const string JwtAudienceMissingMessage = "Jwt:Audience missing";
    private const string JwtKeyMissingMessage = "Jwt:Key missing";
    private const string JwtKeyTooShortMessage = "Jwt:Key must be at least 32 chars (≈256 bits).";

    // ======= Magic numbers =======
    private const int MinKeyLengthChars = 32;           // ~256 bits
    private const int DefaultAccessTokenMinutes = 60;
    private const int DefaultRefreshTokenDays = 30;
    private const int RefreshTokenEntropyBytes = 64;    // 64 bytes de entropía

    public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

    public (string token, DateTime expiresAtUtc) CreateAccessToken(int userId, string? email, IList<string> roles)
    {
        var issuer = _cfg[JwtIssuerKey] ?? throw new InvalidOperationException(JwtIssuerMissingMessage);
        var audience = _cfg[JwtAudienceKey] ?? throw new InvalidOperationException(JwtAudienceMissingMessage);
        var keyRaw = _cfg[JwtKeyKey] ?? throw new InvalidOperationException(JwtKeyMissingMessage);

        if (keyRaw.Length < MinKeyLengthChars) // ~256 bits
            throw new InvalidOperationException(JwtKeyTooShortMessage);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyRaw));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var minutes = int.TryParse(_cfg[JwtAccessTokenMinutesKey], out var m) ? m : DefaultAccessTokenMinutes;
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
        var days = int.TryParse(_cfg[JwtRefreshTokenDaysKey], out var d) ? d : DefaultRefreshTokenDays;
        var expires = DateTime.UtcNow.AddDays(days);

        // 64 bytes de entropía
        var buffer = new byte[RefreshTokenEntropyBytes];
        RandomNumberGenerator.Fill(buffer);

        // Base64URL requiere byte[]
        var rawUrl = Base64UrlEncoder.Encode(buffer);

        return (rawUrl, expires);
    }

    private static string ToUnixSeconds(DateTime utc)
        => ((long)Math.Round((utc - DateTime.UnixEpoch).TotalSeconds)).ToString();
}