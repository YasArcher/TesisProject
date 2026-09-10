using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Interfaces;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedIdentityQueryService(UnifiedDideDbContext context) : IUnifiedIdentityQueryService
{
    public Task<Dictionary<int, string?>> GetEmailsByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToArray();
        return context.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);
    }
    public async Task<(int Id, string? Email)?> GetEmailByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new { u.Id, u.Email }).SingleOrDefaultAsync(ct);
        return user is null ? null : (user.Id, user.Email);
    }
    public Task<Dictionary<int, string?>> GetUsernamesByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToArray();
        return context.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.UserName, ct);
    }
}
