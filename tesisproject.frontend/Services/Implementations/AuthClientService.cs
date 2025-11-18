using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Implementations
{
    public class AuthClientService : IAuthClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/auth"; // coincide con [Route("api/[controller]")]

        public AuthClientService(IApiClient api)
        {
            _api = api;
        }

        public Task<HttpResponseWrapper<AuthResponse?>> LoginAsync(
            LoginRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/auth/login
            return _api.PostAsync<LoginRequest, AuthResponse>(
                $"{_baseUrl}/login",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<AuthResponse?>> RefreshAsync(
            CancellationToken ct = default)
        {
            // POST: api/auth/refresh
            // No necesita body, así que mandamos un objeto vacío {}
            return _api.PostAsync<object, AuthResponse>(
                $"{_baseUrl}/refresh",
                new { },
                ct
            );
        }
    }
}