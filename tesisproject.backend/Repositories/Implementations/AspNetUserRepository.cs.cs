using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;

namespace tesisproject.backend.Repositories.Implementations
{
    public class AspNetUserRepository : GenericRepository<IdentityUser<int>>, IAspNetUserRepository
    {
        private readonly AppDbContext _context;

        public AspNetUserRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // ===== Emails =====
        public async Task<Dictionary<int, string?>> GetEmailsByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        {
            var ids = userIds?.Distinct().ToList() ?? new();
            if (ids.Count == 0)
                return new Dictionary<int, string?>();

            return await _context.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.Email, ct);
        }

        public async Task<(int Id, string? Email)?> GetEmailByUserIdAsync(int userId, CancellationToken ct = default)
        {
            var row = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.Email })
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            return row is null ? null : (row.Id, row.Email);
        }

        // ===== Usernames =====
        public async Task<Dictionary<int, string?>> GetUsernamesByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        {
            var ids = userIds?.Distinct().ToList() ?? new();
            if (ids.Count == 0)
                return new Dictionary<int, string?>();

            return await _context.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.UserName, ct);
        }

        public async Task<(int Id, string? UserName)?> GetUsernameByUserIdAsync(int userId, CancellationToken ct = default)
        {
            var row = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.UserName })
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            return row is null ? null : (row.Id, row.UserName);
        }
        public async Task<IReadOnlyList<string>> GetAllUsernamesAsync(CancellationToken ct = default)
    => await _db.AsNoTracking()
                .Select(u => u.UserName!)
                .Where(u => u != null && u != "")
                .Distinct()
                .ToListAsync(ct);
    }
}