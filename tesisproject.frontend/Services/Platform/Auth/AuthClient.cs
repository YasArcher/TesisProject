using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Auth;
public class AuthClient : IAuthClient
{
    private readonly HttpClient _http;
    private readonly ITokenStore _store;

    public AuthClient(HttpClient http, ITokenStore store)
    {
        _http = http;
        _store = store;
    }

    public async Task<LoguinResponse?> LoginAsync(LoguinRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/login", req);
        if (!resp.IsSuccessStatusCode) return null;

        var dto = await resp.Content.ReadFromJsonAsync<LoguinResponse>();
        if (dto is null || string.IsNullOrWhiteSpace(dto.AccessToken)) return null;

        await _store.SetAsync(dto.AccessToken);
        return dto;
    }

    public async Task<LoguinResponse?> RegisterAsync(RegisterUserRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/register", req);
        if (!resp.IsSuccessStatusCode) return null;

        var dto = await resp.Content.ReadFromJsonAsync<LoguinResponse>();
        // si tu /register retorna token, guárdalo; si no, omite SetAsync
        if (dto is not null && !string.IsNullOrWhiteSpace(dto.AccessToken))
            await _store.SetAsync(dto.AccessToken);

        return dto;
    }

    public Task<AuthMeResponse?> GetCurrentAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<AuthMeResponse>("api/auth/me", ct);
}