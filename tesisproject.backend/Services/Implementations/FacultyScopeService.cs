using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Entities.Auth;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class FacultyScopeService : IFacultyScopeService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAppUserService _appUsers;
        private const string TemporaryPassword = "Temp123*";

        public FacultyScopeService(IUnitOfWork uow, IAppUserService appUsers)
        {
            _uow = uow;
            _appUsers = appUsers;
        }

        public async Task<ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>> GetAllAsync(
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            var q = _uow.FacultyScopes.QueryWithRefs(includeAssignments, asNoTracking: true);

            var list = await q.ToListAsync(ct);
            var mapped = list.Select(x => Map(x, includeAssignments)).ToList();

            return ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>.Ok(mapped);
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> GetByIdAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("FacultyScopeId is required.", ErrorType.Validation);

            var e = await _uow.FacultyScopes.GetByIdWithRefsAsync(facultyScopeId, includeAssignments, ct);
            if (e is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Faculty scope not found.", ErrorType.NotFound);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(Map(e, includeAssignments));
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> CreateAsync(
            CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Request is required.", ErrorType.Validation);

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Name is required.", ErrorType.Validation);

            // evita duplicado por nombre (tienes índice unique)
            var exists = await _uow.FacultyScopes.ExistsAsync(x => x.Name == name, ct);
            if (exists)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("A scope with the same name already exists.", ErrorType.Conflict);

            // normaliza faculties iniciales
            var facultyIds = NormalizeFacultyIds(request.FacultyIds);
            if (facultyIds.Invalid.Count > 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(
                    $"Invalid FacultyIds: {string.Join(", ", facultyIds.Invalid)}",
                    ErrorType.Validation);

            var scope = new FacultyScope
            {
                Name = name,
                IsActive = true
            };

            await _uow.FacultyScopes.AddAsync(scope, ct);
            await _uow.SaveChangesAsync(ct); // para obtener FacultyScopeId

            // Cargar faculties iniciales (si vienen)
            if (facultyIds.Ids.Count > 0)
            {
                var rows = facultyIds.Ids.Select(fid => new FacultyScopeFaculty
                {
                    FacultyScopeId = scope.FacultyScopeId,
                    FacultyId = fid,
                    IsActive = true
                });

                await _uow.FacultyScopeFaculties.AddRangeAsync(rows, ct);
                await _uow.SaveChangesAsync(ct);
            }

            var refreshed = await _uow.FacultyScopes.GetByIdWithRefsAsync(
                scope.FacultyScopeId,
                includeAssignments: false,
                ct);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(Map(refreshed!, includeAssignments: false));
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> UpdateAsync(
            int facultyScopeId,
            UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("FacultyScopeId is required.", ErrorType.Validation);

            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Request is required.", ErrorType.Validation);

            var e = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
            if (e is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Faculty scope not found.", ErrorType.NotFound);

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Name is required.", ErrorType.Validation);

            // si cambia el nombre, valida duplicado
            if (!string.Equals(e.Name, name, StringComparison.Ordinal))
            {
                var exists = await _uow.FacultyScopes.ExistsAsync(x => x.Name == name, ct);
                if (exists)
                    return ServiceResult<FacultyScopeResponseDTO>.Fail("A scope with the same name already exists.", ErrorType.Conflict);

                e.Name = name;
            }

            if (request.IsActive.HasValue)
                e.IsActive = request.IsActive.Value;

            await _uow.SaveChangesAsync(ct);

            var refreshed = await _uow.FacultyScopes.GetByIdWithRefsAsync(
                facultyScopeId,
                includeAssignments: false,
                ct);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(Map(refreshed!, includeAssignments: false));
        }

        /// <summary>
        /// Reemplazo/bulk: deja EXACTAMENTE las FacultyIds enviadas como activas,
        /// desactiva las demás, y crea las nuevas si no existían.
        /// </summary>
        public async Task<ServiceResult<FacultyScopeResponseDTO>> SetFacultiesAsync(
            int facultyScopeId,
            SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("FacultyScopeId is required.", ErrorType.Validation);

            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Request is required.", ErrorType.Validation);

            var scope = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
            if (scope is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail("Faculty scope not found.", ErrorType.NotFound);

            var normalized = NormalizeFacultyIds(request.FacultyIds);
            if (normalized.Invalid.Count > 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(
                    $"Invalid FacultyIds: {string.Join(", ", normalized.Invalid)}",
                    ErrorType.Validation);

            var target = normalized.Ids;

            // Trae existentes (activos e inactivos) para ese scope
            var existing = await _uow.FacultyScopeFaculties
                .GetAllAsync(x => x.FacultyScopeId == facultyScopeId, ct);

            var byId = existing.ToDictionary(x => x.FacultyId);

            // 1) activar los target, crear los que no existan
            foreach (var fid in target)
            {
                if (byId.TryGetValue(fid, out var row))
                {
                    if (!row.IsActive) row.IsActive = true;
                }
                else
                {
                    await _uow.FacultyScopeFaculties.AddAsync(new FacultyScopeFaculty
                    {
                        FacultyScopeId = facultyScopeId,
                        FacultyId = fid,
                        IsActive = true
                    }, ct);
                }
            }

            // 2) desactivar los que ya no están en target
            var targetSet = new HashSet<int>(target);
            foreach (var row in existing)
            {
                if (!targetSet.Contains(row.FacultyId) && row.IsActive)
                    row.IsActive = false;
            }

            await _uow.SaveChangesAsync(ct);

            var refreshed = await _uow.FacultyScopes.GetByIdWithRefsAsync(
                facultyScopeId,
                includeAssignments: false,
                ct);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(Map(refreshed!, includeAssignments: false));
        }

        public async Task<ServiceResult<bool>> AssignScopeToUserAsync(
                    int facultyScopeId,
                    AssignFacultyScopeUserRequestDTO request,
                    CancellationToken ct = default)
        {
            try
            {
                if (facultyScopeId <= 0)
                    return ServiceResult<bool>.Fail("FacultyScopeId is required.", ErrorType.Validation);

                if (request is null)
                    return ServiceResult<bool>.Fail("Request is required.", ErrorType.Validation);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<bool>.Fail("Institutional email is required.", ErrorType.Validation);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<bool>.Fail("Document is required.", ErrorType.Validation);

                var scope = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
                if (scope is null)
                    return ServiceResult<bool>.Fail("Faculty scope not found.", ErrorType.NotFound);

                // Igual que en GroupService.AddMemberAsync:
                // resuelve/crea AppUser local a partir de Email + Document + AspUserId
                var registerDto = new RegisterRequest
                {
                    Email = email,
                    Username = document,
                    Password = TemporaryPassword,
                    AspUserId = request.AspUserId,
                    Role = AppRoles.User
                };

                var ensureResult = await _appUsers.EnsureAppUserAsync(registerDto, ct);
                if (!ensureResult.Success)
                {
                    return ServiceResult<bool>.Fail(
                        ensureResult.Message ?? "Failed to ensure AppUser.",
                        ensureResult.Error);
                }

                var appUserPk = ensureResult.Data;

                var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserPk }, ct);
                if (appUser is null)
                    return ServiceResult<bool>.Fail("Unable to resolve AppUser.", ErrorType.Unexpected);

                // Ojo: aunque la columna se llama IdentityUserId,
                // el valor real que estás guardando aquí es AppUsers.IdUser
                var identityUserId = appUser.IdUser;

                var key = new object[] { identityUserId, facultyScopeId };
                var existing = await _uow.UserFacultyScopeAssignments.GetByIdAsync(key, ct);

                if (existing is null)
                {
                    await _uow.UserFacultyScopeAssignments.AddAsync(new UserFacultyScopeAssignment
                    {
                        IdentityUserId = identityUserId,
                        FacultyScopeId = facultyScopeId,
                        IsActive = true
                    }, ct);
                }
                else
                {
                    existing.IsActive = true;
                }

                await _uow.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(true, "Scope assigned.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<bool>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<bool>> UnassignScopeFromUserAsync(
            int facultyScopeId,
            int identityUserId,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<bool>.Fail("FacultyScopeId is required.", ErrorType.Validation);

            if (identityUserId <= 0)
                return ServiceResult<bool>.Fail("IdentityUserId is required.", ErrorType.Validation);

            var key = new object[] { identityUserId, facultyScopeId };
            var existing = await _uow.UserFacultyScopeAssignments.GetByIdAsync(key, ct);

            if (existing is null)
                return ServiceResult<bool>.Ok(true, "Assignment not found (already unassigned).");

            existing.IsActive = false;
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true, "Scope unassigned.");
        }

        public async Task<ServiceResult<IReadOnlyList<int>>> GetAllowedFacultyIdsForUserAsync(
            int identityUserId,
            CancellationToken ct = default)
        {
            if (identityUserId <= 0)
                return ServiceResult<IReadOnlyList<int>>.Fail("IdentityUserId is required.", ErrorType.Validation);

            var user = await _uow.AppUsers.GetByIdUserAsync(identityUserId, ct);
            if (user is null)
                return ServiceResult<IReadOnlyList<int>>.Fail("User not found.", ErrorType.NotFound);

            // Debe devolver IReadOnlyList<int> desde el repo
            var ids = await _uow.UserFacultyScopeAssignments.GetActiveFacultyIdsByUserAsync(identityUserId, ct);
            return ServiceResult<IReadOnlyList<int>>.Ok(ids);
        }

        // ========================= Helpers =========================

        private sealed record NormalizedFacultyIds(List<int> Ids, List<string> Invalid);

        private static NormalizedFacultyIds NormalizeFacultyIds(IEnumerable<int>? raw)
        {
            var ids = new List<int>();
            var invalid = new List<string>();

            if (raw is null) return new NormalizedFacultyIds(ids, invalid);

            foreach (var id in raw)
            {
                if (id > 0) ids.Add(id);
                else invalid.Add(id.ToString());
            }

            ids = ids.Distinct().ToList();
            return new NormalizedFacultyIds(ids, invalid);
        }

        private static FacultyScopeResponseDTO Map(FacultyScope e, bool includeAssignments)
        {
            var dto = new FacultyScopeResponseDTO
            {
                FacultyScopeId = e.FacultyScopeId,
                Name = e.Name,
                IsActive = e.IsActive,
                Faculties = e.Faculties?
                    .Select(f => new FacultyScopeFacultyItemDTO
                    {
                        FacultyId = f.FacultyId, // int
                        IsActive = f.IsActive
                    })
                    .OrderBy(x => x.FacultyId)
                    .ToList() ?? new List<FacultyScopeFacultyItemDTO>()
            };

            if (includeAssignments && e.UserAssignments is not null)
            {
                dto.AssignedUserIds = e.UserAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.IdentityUserId)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            return dto;
        }
    }
}