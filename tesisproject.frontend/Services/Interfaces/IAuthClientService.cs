using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IAuthClientService
    {
        Task<HttpResponseWrapper<AuthResponse?>> LoginAsync(
            LoginRequest request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<AuthResponse?>> RefreshAsync(
            CancellationToken ct = default);
        Task<HttpResponseWrapper<CurrentSessionResponse?>> GetCurrentSessionAsync(
            CancellationToken ct = default);
    }
}
