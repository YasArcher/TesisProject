using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)> RegisterAsync(RegisterRequest dto, string? ip, CancellationToken ct);
        Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)> LoginAsync(LoginRequest dto, string? ip, CancellationToken ct);
        Task<(AuthResponse? data, int statusCode, string? error, (string token, DateTime exp)? refreshCookie)> RefreshAsync(string? refreshCookie, string? ip, CancellationToken ct);
        Task RevokeAsync(string? refreshCookie, string? ip, CancellationToken ct);
    }
}
