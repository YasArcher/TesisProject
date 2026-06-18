using System.Security.Claims;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Interfaces;

public interface IAuthService
{
    Task<LoguinResponse> RegisterAsync(RegisterUserRequest request);
    Task<LoguinResponse> LoginAsync(LoguinRequest request);
    Task<AuthMeResponse> MeAsync(ClaimsPrincipal user);
    Task<AuthMeResponse> AcceptTermsAsync(ClaimsPrincipal user, AcceptTermsRequest request, CancellationToken ct = default);
}
