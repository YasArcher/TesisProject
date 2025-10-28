using System.Security.Claims;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Interfaces;

public interface IAuthClient
{
    Task<LoguinResponse?> LoginAsync(LoguinRequest req);
    Task<LoguinResponse?> RegisterAsync(RegisterUserRequest req);

    // /api/auth/me
    Task<AuthMeResponse?> GetCurrentAsync(CancellationToken ct = default);
}