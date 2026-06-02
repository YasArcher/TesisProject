using System.Net.Http.Json;
using tesisproject.frontend.Services.Errors;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Implementations;

public class IdentityAdministrationClient : IIdentityAdministrationClient
{
    private readonly HttpClient _http;

    public IdentityAdministrationClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default)
        => await GetAsync<List<IdentityUserListItemDto>>("api/admin/security/users", ct) ?? [];

    public async Task<IReadOnlyList<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default)
        => await GetAsync<List<IdentityRoleListItemDto>>("api/admin/security/roles", ct) ?? [];

    public async Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default)
        => await SendAsync<IdentityUserListItemDto>(HttpMethod.Post, "api/admin/security/users", request, ct);

    public async Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default)
        => await SendAsync<IdentityRoleListItemDto>(HttpMethod.Post, "api/admin/security/roles", request, ct);

    public async Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default)
        => await SendAsync<IdentityUserListItemDto>(HttpMethod.Put, $"api/admin/security/users/{userId}/roles", request, ct);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildErrorMessageAsync(response, ct));
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildErrorMessageAsync(response, ct));
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return payload ?? throw new InvalidOperationException("La API no devolvió un resultado válido.");
    }

    private static async Task<string> BuildErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        => await UserFacingErrorMapper.FromHttpResponseAsync(response, "La operación no se pudo completar. Revisa los datos e inténtalo nuevamente.", ct);
}
