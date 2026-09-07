using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedIdentityProvisioningService
{
    // Resolve a known business user without provisioning or changing roles.
    Task<ServiceResult<int?>> ResolveAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<int>> EnsureAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<List<int>>> EnsureSelectedAsync(IEnumerable<RegisterRequest> requests, CancellationToken ct = default);
}
