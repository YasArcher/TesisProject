using System.Net;
using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalUsersService : IExternalUsersService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalUsersService> _logger;

        public ExternalUsersService(HttpClient http, ILogger<ExternalUsersService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<bool> UserExistsAsync(int externalUserId, CancellationToken ct)
        {
            using var resp = await _http.GetAsync($"usuarios/{externalUserId}", ct);
            if (resp.StatusCode == HttpStatusCode.NotFound) return false;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("External Users API returned {Status} for id {Id}", resp.StatusCode, externalUserId);
                resp.EnsureSuccessStatusCode(); // lanza si 5xx/4xx distinto de 404
            }
            return true;
        }

        // (opcional) si luego necesitas datos del usuario:
        // public async Task<ExternalUserDto?> GetUserAsync(int id, CancellationToken ct)
        // {
        //     var resp = await _http.GetAsync($"usuarios/{id}", ct);
        //     if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        //     resp.EnsureSuccessStatusCode();
        //     return await resp.Content.ReadFromJsonAsync<ExternalUserDto>(cancellationToken: ct);
        // }
    }

    // (opcional) Ajusta a lo que devuelva tu API real
    // public record ExternalUserDto(int id, string nombre, string email);
}
