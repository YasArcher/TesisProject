namespace tesisproject.backend.Services.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(string action, string? userId, string? entity, string? entityId, string? detail, CancellationToken ct);
    }
}
