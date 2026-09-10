using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Auth;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedRefreshTokenService
    {
        Task<RefreshToken> CreateAsync(int userId, string token, DateTime expires, string? createdByIp, CancellationToken ct);
        Task<RefreshToken?> GetActiveAsync(int userId, string token, CancellationToken ct);
        Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken ct);
        Task RevokeAsync(RefreshToken rt, string? byIp, string? replacedByToken, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
