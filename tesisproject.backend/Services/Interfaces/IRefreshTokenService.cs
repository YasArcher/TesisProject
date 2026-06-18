using tesisproject.shared.Entities.Auth;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> CreateAsync(int userId, string token, DateTime expires, string? createdByIp, CancellationToken ct);
        Task<RefreshToken?> GetActiveAsync(int userId, string token, CancellationToken ct);
        Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken ct);
        Task RevokeAsync(RefreshToken rt, string? byIp, string? replacedByToken, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
