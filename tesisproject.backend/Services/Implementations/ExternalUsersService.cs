using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

public class ExternalUsersService : IExternalUsersService
{
    private readonly HttpClient _http;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExternalUsersService> _logger;
    private readonly ExternalApiOptions _opts;

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public ExternalUsersService(
        HttpClient http,
        IUnitOfWork uow,
        IOptions<ExternalApiOptions> opts,
        ILogger<ExternalUsersService> logger)
    {
        _http = http;
        _uow = uow;
        _logger = logger;
        _opts = opts.Value;

        // Salvaguarda: si el cliente nombrado no seteó BaseAddress por config:
        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
            _http.BaseAddress = new Uri(_opts.BaseUrl);
    }

    // ============== READS ==============

    public async Task<ServiceResult<ExternalUserDTO>> GetByIdAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<ExternalUserApiModel>(
                $"{_opts.UsersEndpoint}/{userId}", _jsonOpts, ct);

            if (api is null)
            {
                _logger.LogWarning("External user {UserId} returned null payload from external API", userId);
                return ServiceResult<ExternalUserDTO>.Fail("External user not found.", ErrorType.NotFound);
            }

            var dto = MapToDto(api, role: null, memberId: 0, groupId: 0);
            _logger.LogInformation("External user {UserId} retrieved from external API", userId);

            return ServiceResult<ExternalUserDTO>.Ok(dto, "External user retrieved");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "External user {UserId} not found (404)", userId);
            return ServiceResult<ExternalUserDTO>.Fail("External user not found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving external user {UserId}", userId);
            return ServiceResult<ExternalUserDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
    }

    public async Task<ServiceResult<List<ExternalUserDTO>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<List<ExternalUserApiModel>>(
                _opts.UsersEndpoint, _jsonOpts, ct);

            if (api is null || api.Count == 0)
            {
                _logger.LogWarning("External API returned empty list for {Endpoint}", _opts.UsersEndpoint);
                return ServiceResult<List<ExternalUserDTO>>.Fail("No external users found.", ErrorType.NotFound);
            }

            var list = api
                .Where(u => u is not null)
                .Select(u => MapToDto(u, role: null, memberId: 0, groupId: 0))
                .ToList();

            _logger.LogInformation("Retrieved {Count} external users from API", list.Count);
            return ServiceResult<List<ExternalUserDTO>>.Ok(list, "External users retrieved");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "External API returned 404 for {Endpoint}", _opts.UsersEndpoint);
            return ServiceResult<List<ExternalUserDTO>>.Fail("No external users found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving external users");
            return ServiceResult<List<ExternalUserDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
    }

    public async Task<ServiceResult<List<ExternalUserDTO>>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
    {
        try
        {
            // 1) Miembros del grupo (local DB)
            var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct);
            if (members is null || members.Count == 0)
            {
                _logger.LogInformation("Group {GroupId} has no local members", groupId);
                return ServiceResult<List<ExternalUserDTO>>.Fail("No group members found.", ErrorType.NotFound);
            }

            // 2) Lookups locales
            var externalIds = members.Select(m => m.UserId).ToHashSet();
            var roleByExternalId = members.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.First().MemberRole);
            var memberIdByExternalId = members.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.First().GroupMemberId);

            // 3) Usuarios externos (todos) -> podrías optimizar si el endpoint permite filtro por IDs
            List<ExternalUserApiModel>? allUsers;
            try
            {
                allUsers = await _http.GetFromJsonAsync<List<ExternalUserApiModel>>(
                    _opts.UsersEndpoint, _jsonOpts, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "External API returned 404 for {Endpoint} (group {GroupId})", _opts.UsersEndpoint, groupId);
                allUsers = new List<ExternalUserApiModel>();
            }

            if (allUsers is null || allUsers.Count == 0)
            {
                return ServiceResult<List<ExternalUserDTO>>.Fail("No external users available.", ErrorType.NotFound);
            }

            // 4) Intersección con miembros locales
            var filtered = allUsers.Where(u => u is not null && externalIds.Contains(u.id_usuario));

            // 5) Mapear con rol y GroupMemberId
            var result = filtered.Select(u =>
            {
                roleByExternalId.TryGetValue(u.id_usuario, out var role);
                memberIdByExternalId.TryGetValue(u.id_usuario, out var memberId);
                return MapToDto(u, role, memberId, groupId);
            }).ToList();

            if (result.Count == 0)
                return ServiceResult<List<ExternalUserDTO>>.Fail("No external users matched the group members.", ErrorType.NotFound);

            _logger.LogInformation("Retrieved {Count} external users for group {GroupId}", result.Count, groupId);
            return ServiceResult<List<ExternalUserDTO>>.Ok(result, "External users by group retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving external users for group {GroupId}", groupId);
            return ServiceResult<List<ExternalUserDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
    }

    // ---------- Mapping ----------
    private static ExternalUserDTO MapToDto(
        ExternalUserApiModel u,
        string? role,
        int memberId,   // GroupMemberId en tu DB local
        int groupId     // GroupId actual
    ) => new ExternalUserDTO
    {
        UserId = u.id_usuario,
        GroupId = groupId,
        UserGroupId = memberId,
        FullName = u.Nombre,
        Document = u.Cedula,
        Phone = u.Celular,
        Email = u.Correo,
        Position = u.Cargo,
        FacultyCareerId = u.id_facultad_carrera,
        Role = role
    };
}
