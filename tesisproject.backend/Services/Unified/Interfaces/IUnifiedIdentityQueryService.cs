namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedIdentityQueryService
{
    Task<Dictionary<int, string?>> GetEmailsByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default);
    Task<(int Id, string? Email)?> GetEmailByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Dictionary<int, string?>> GetUsernamesByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default);
}
