using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.Entities.External;

public class ExternalUsersService : IExternalUsersService
{
    private readonly HttpClient _http;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExternalUsersService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public ExternalUsersService(HttpClient http, IUnitOfWork uow, IOptions<ExternalApiOptions> opts, ILogger<ExternalUsersService> logger)
    {
        _http = http;
        _uow = uow;
        _logger = logger;

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(opts.Value.UsersBaseUrl))
            _http.BaseAddress = new Uri(opts.Value.UsersBaseUrl);
    }

    public async Task<ExternalUserDTO?> GetByIdAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<ExternalUserApiModel>(
                $"/api/usuarios/{userId}", _jsonOpts, ct);

            return api is null
                ? null
                : MapToDto(api, role: null, memberId: 0, groupId: 0);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("User {UserId} not found in external API", userId);
            return null;
        }
    }


    public async Task<List<ExternalUserDTO>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
    {
        // 1) Miembros del grupo (local DB)
        var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct);
        if (members is null || members.Count == 0)
            return new List<ExternalUserDTO>();

        // 2) Proyecta a IDs externos y arma lookup de roles
        var externalIds = members.Select(m => m.UserId).ToHashSet();

        var roleByExternalId = members
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First().MemberRole);

        var groupIdByExternalId = members
    .GroupBy(m => m.UserId)
    .ToDictionary(g => g.Key, g => g.First().GroupMemberId);

        // 3) Trae todos los usuarios externos
        List<ExternalUserApiModel>? allUsers;
        try
        {
            allUsers = await _http.GetFromJsonAsync<List<ExternalUserApiModel>>("/api/usuarios", _jsonOpts, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("External API returned 404 for /api/usuarios");
            allUsers = new List<ExternalUserApiModel>();
        }

        if (allUsers is null || allUsers.Count == 0)
            return new List<ExternalUserDTO>();

        // 4) Intersección
        var filteredList = allUsers.Where(u => u is not null && externalIds.Contains(u.id_usuario));

        // 5) Mapear pasando el rol (si no existe, null o string vacío)
        return filteredList
            .Select(u =>
            {
                roleByExternalId.TryGetValue(u.id_usuario, out var role);
                groupIdByExternalId.TryGetValue(u.id_usuario, out var gId);
                return MapToDto(u, role, gId, groupId);
            })
            .ToList();
    }


    // ---------- Mapeo ----------
    private static ExternalUserDTO MapToDto(
        ExternalUserApiModel u,
        string? role,
        int memberId,   // <- GroupMember.Id
        int groupId     // <- parámetro del método (Group.Id)
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