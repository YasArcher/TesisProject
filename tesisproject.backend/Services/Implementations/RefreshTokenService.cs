using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.Auth;

namespace tesisproject.backend.Services.Implementations
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _db;
        private readonly DbSet<RefreshToken> _set;
        public RefreshTokenService(AppDbContext db)
        {
            _db = db;
            _set = _db.Set<RefreshToken>();
        }

        private static string Hash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        public async Task<RefreshToken> CreateAsync(int userId, string token, DateTime expires, string? ip, CancellationToken ct)
        {
            var rt = new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(token),
                ExpiresAtUtc = expires,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIp = ip
            };
            _set.Add(rt);
            await _db.SaveChangesAsync(ct);
            return rt;
        }

        public Task<RefreshToken?> GetActiveAsync(int userId, string token, CancellationToken ct)
        {
            var h = Hash(token);
            return _set.AsNoTracking().FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.TokenHash == h &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > DateTime.UtcNow, ct);
        }

        public Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken ct)
        {
            var h = Hash(token);
            return _set.AsNoTracking().FirstOrDefaultAsync(x =>
                x.TokenHash == h &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > DateTime.UtcNow, ct);
        }

        public async Task RevokeAsync(RefreshToken rt, string? byIp, string? replacedByToken, CancellationToken ct)
        {
            rt.RevokedAtUtc = DateTime.UtcNow;
            rt.RevokedByIp = byIp;
            rt.ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacedByToken) ? null : Hash(replacedByToken);

            _set.Attach(rt);
            _db.Entry(rt).Property(p => p.RevokedAtUtc).IsModified = true;
            _db.Entry(rt).Property(p => p.RevokedByIp).IsModified = true;
            _db.Entry(rt).Property(p => p.ReplacedByTokenHash).IsModified = true;

            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
