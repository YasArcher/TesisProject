using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedAuthService
    {
        Task<(ServiceResult<AuthResponse> Result, (string token, DateTime exp)? RefreshCookie)>
            RegisterAsync(RegisterRequest dto, string? ip, CancellationToken ct);

        Task<(ServiceResult<AuthResponse> Result, (string token, DateTime exp)? RefreshCookie)>
            LoginAsync(LoginRequest dto, string? ip, CancellationToken ct);

        Task<(ServiceResult<AuthResponse> Result, (string token, DateTime exp)? RefreshCookie)>
            RefreshAsync(string? refreshCookie, string? ip, CancellationToken ct);

        Task<ServiceResult<NoContent>> RevokeAsync(string? refreshCookie,string? ip,CancellationToken ct);
    }
}
