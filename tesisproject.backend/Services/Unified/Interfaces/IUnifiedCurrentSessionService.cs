using System.Security.Claims;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedCurrentSessionService
{
    Task<ServiceResult<CurrentSessionResponse>> GetAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}
