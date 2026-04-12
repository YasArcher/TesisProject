using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.frontend.Services.Platform.Auth;

public class IdentityAdministrationClient : IIdentityAdministrationClient
{
    private readonly IApiClient _api;

    public IdentityAdministrationClient(IApiClient api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<IdentityUserListItemDto>> GetUsersAsync(CancellationToken ct = default)
        => (await GetUsersResultAsync(ct)).Data ?? [];

    public async Task<IReadOnlyList<IdentityRoleListItemDto>> GetRolesAsync(CancellationToken ct = default)
        => (await GetRolesResultAsync(ct)).Data ?? [];

    public async Task<IdentityUserListItemDto> CreateUserAsync(CreateIdentityUserRequest request, CancellationToken ct = default)
        => await RequireDataAsync(
            CreateUserResultAsync(request, ct),
            "La API no devolvió un resultado válido al crear el usuario.");

    public async Task<IdentityRoleListItemDto> CreateRoleAsync(CreateIdentityRoleRequest request, CancellationToken ct = default)
        => await RequireDataAsync(
            CreateRoleResultAsync(request, ct),
            "La API no devolvió un resultado válido al crear el rol.");

    public async Task<IdentityUserListItemDto> UpdateUserRolesAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default)
        => await RequireDataAsync(
            UpdateUserRolesResultAsync(userId, request, ct),
            "La API no devolvió un resultado válido al actualizar los roles del usuario.");

    public async Task<HttpResponseWrapper<IReadOnlyList<IdentityUserListItemDto>?>> GetUsersResultAsync(CancellationToken ct = default)
    {
        var result = await _api.GetResultAsync<List<IdentityUserListItemDto>>("api/admin/security/users", ct);
        return result.Success
            ? HttpResponseWrapper<IReadOnlyList<IdentityUserListItemDto>?>.Ok(result.Data ?? [], result.StatusCode, result.Message)
            : HttpResponseWrapper<IReadOnlyList<IdentityUserListItemDto>?>.Fail(result.Message, result.StatusCode, result.ErrorCode, result.ValidationErrors);
    }

    public async Task<HttpResponseWrapper<IReadOnlyList<IdentityRoleListItemDto>?>> GetRolesResultAsync(CancellationToken ct = default)
    {
        var result = await _api.GetResultAsync<List<IdentityRoleListItemDto>>("api/admin/security/roles", ct);
        return result.Success
            ? HttpResponseWrapper<IReadOnlyList<IdentityRoleListItemDto>?>.Ok(result.Data ?? [], result.StatusCode, result.Message)
            : HttpResponseWrapper<IReadOnlyList<IdentityRoleListItemDto>?>.Fail(result.Message, result.StatusCode, result.ErrorCode, result.ValidationErrors);
    }

    public Task<HttpResponseWrapper<IdentityUserListItemDto?>> CreateUserResultAsync(CreateIdentityUserRequest request, CancellationToken ct = default)
        => _api.PostResultAsync<CreateIdentityUserRequest, IdentityUserListItemDto>("api/admin/security/users", request, ct);

    public Task<HttpResponseWrapper<IdentityRoleListItemDto?>> CreateRoleResultAsync(CreateIdentityRoleRequest request, CancellationToken ct = default)
        => _api.PostResultAsync<CreateIdentityRoleRequest, IdentityRoleListItemDto>("api/admin/security/roles", request, ct);

    public Task<HttpResponseWrapper<IdentityUserListItemDto?>> UpdateUserRolesResultAsync(string userId, UpdateIdentityUserRolesRequest request, CancellationToken ct = default)
        => _api.PutResultAsync<UpdateIdentityUserRolesRequest, IdentityUserListItemDto>($"api/admin/security/users/{userId}/roles", request, ct);

    private static async Task<T> RequireDataAsync<T>(Task<HttpResponseWrapper<T?>> resultTask, string fallbackMessage)
        where T : class
    {
        var result = await resultTask;
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message ?? fallbackMessage);
        }

        return result.Data ?? throw new InvalidOperationException(fallbackMessage);
    }
}
