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

        // [ARTICLES-MIGRATION] Consulta la sesion unificada sin modificar el flujo de login.
        Task<HttpResponseWrapper<CurrentSessionResponse?>> GetCurrentSessionAsync(
            CancellationToken ct = default);
    }
}
