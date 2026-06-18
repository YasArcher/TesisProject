using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Implementations;
    public class AuditLogger : IAuditLogger
    {
        private readonly AppDbContext _db;
        public AuditLogger(AppDbContext db) => _db = db;

        public async Task LogAsync(string action, string? userId, string? entity, string? entityId, string? detail, CancellationToken ct)
        {
            _db.AuditLogs.Add(new Data.Entities.AuditLog
            {
                AtUtc = DateTime.UtcNow,
                UserId = userId,
                Action = action,
                EntityName = entity,
                EntityId = entityId,
                Detail = detail
            });
            await _db.SaveChangesAsync(ct);
        }
    }

