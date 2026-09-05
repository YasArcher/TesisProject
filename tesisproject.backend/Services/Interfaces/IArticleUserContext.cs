namespace tesisproject.backend.Services.Interfaces;

public interface IArticleUserContext
{
    bool IsAuthenticated { get; }
    int? IdentityUserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }

    bool IsInRole(string role);
    bool HasPermission(string permission);
    Task<int?> GetAppUserIdAsync(CancellationToken ct = default);
    Task<int> GetRequiredAppUserIdAsync(CancellationToken ct = default);
}
