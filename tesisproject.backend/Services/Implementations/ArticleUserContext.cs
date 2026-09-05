using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.Auth;
using tesisproject.shared.Auth.Articles;

namespace tesisproject.backend.Services.Implementations;

public sealed class ArticleUserContext : IArticleUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAppUserRepository _appUsers;
    private bool _appUserResolved;
    private int? _appUserId;

    public ArticleUserContext(
        IHttpContextAccessor httpContextAccessor,
        IAppUserRepository appUsers)
    {
        _httpContextAccessor = httpContextAccessor;
        _appUsers = appUsers;
    }

    private ClaimsPrincipal Principal
        => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public int? IdentityUserId => Principal.GetUserId();

    public string? Email
        => FindFirstValue(JwtRegisteredClaimNames.Email, ClaimTypes.Email);

    public string? DisplayName
        => FindFirstValue("name", ClaimTypes.Name) ?? Email;

    public IReadOnlyCollection<string> Roles
        => ReadDistinctClaims("role", ClaimTypes.Role);

    public IReadOnlyCollection<string> Permissions
        => ReadDistinctClaims(ArticlePermissions.ClaimType);

    public bool IsInRole(string role)
        => !string.IsNullOrWhiteSpace(role) && Principal.IsInRole(role);

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        return IsInRole(AppRoles.Admin) ||
               IsInRole(AppRoles.SuperAdmin) ||
               Principal.HasClaim(ArticlePermissions.ClaimType, permission);
    }

    public async Task<int?> GetAppUserIdAsync(CancellationToken ct = default)
    {
        if (_appUserResolved)
            return _appUserId;

        _appUserResolved = true;
        var identityUserId = IdentityUserId;
        if (!identityUserId.HasValue)
            return null;

        var appUser = await _appUsers.GetByLocalIdAsync(identityUserId.Value, ct);
        _appUserId = appUser?.IdUser;
        return _appUserId;
    }

    public async Task<int> GetRequiredAppUserIdAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            throw new UnauthorizedAccessException("ARTICLE_USER_NOT_AUTHENTICATED");

        var appUserId = await GetAppUserIdAsync(ct);
        if (!appUserId.HasValue)
            throw new UnauthorizedAccessException("ARTICLE_APP_USER_NOT_LINKED");

        return appUserId.Value;
    }

    private string? FindFirstValue(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = Principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private IReadOnlyCollection<string> ReadDistinctClaims(params string[] claimTypes)
        => Principal.Claims
            .Where(claim => claimTypes.Contains(claim.Type, StringComparer.Ordinal))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
