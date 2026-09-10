using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedFacultyScopeService : IUnifiedFacultyScopeService
    {
        private readonly IUnifiedUnitOfWork _uow;
        private readonly IUnifiedIdentityProvisioningService _identity;
        private readonly ICurrentUserService _currentUser;

















        public UnifiedFacultyScopeService(
            IUnifiedUnitOfWork uow,
            ICurrentUserService currentUser,
            IUnifiedIdentityProvisioningService identity)
        {
            _uow = uow;
            _identity = identity;
            _currentUser = currentUser;
        }

        public async Task<ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>> GetAllAsync(
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            var q = _uow.FacultyScopes.QueryWithRefs(includeAssignments, asNoTracking: true);

            var list = await q.ToListAsync(ct);
            var mapped = new List<FacultyScopeResponseDTO>();
            foreach (var scope in list) mapped.Add(await MapAsync(scope, includeAssignments, ct));

            return ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>.Ok(mapped);
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> GetByIdAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var e = await _uow.FacultyScopes.GetByIdWithRefsAsync(facultyScopeId, includeAssignments, ct);
            if (e is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(await MapAsync(e, includeAssignments, ct));
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> CreateAsync(
            UnifiedCreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var exists = await _uow.FacultyScopes.ExistsAsync(x => x.Name == name, ct);
            if (exists)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_ScopeNameAlreadyExistsMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

            var resolved = await UnifiedAcademicReferencePreparation.FacultiesAsync(_uow, request.ExternalFacultyIds, ct);
            if (!resolved.Success) return UnifiedAcademicReferencePreparation.Relay<FacultyScopeResponseDTO, List<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>>(resolved);

            var scope = new FacultyScope
            {
                Name = name,
                IsActive = true
            };

            scope.Faculties = resolved.Data!.Select(f => new FacultyScopeFaculty
            {
                FacultyScope = scope, FacultyId = f.FacultyId, IsActive = true
            }).ToList();
            await _uow.FacultyScopes.AddAsync(scope, ct);
            await _uow.SaveChangesAsync(ct);

            var refreshed = await _uow.FacultyScopes.GetByIdWithRefsAsync(
                scope.FacultyScopeId,
                includeAssignments: false,
                ct);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(await MapAsync(refreshed!, includeAssignments: false, ct));
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> UpdateAsync(
            int facultyScopeId,
            UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var e = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
            if (e is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            if (!string.Equals(e.Name, name, StringComparison.Ordinal))
            {
                var exists = await _uow.FacultyScopes.ExistsAsync(x => x.Name == name, ct);
                if (exists)
                    return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_ScopeNameAlreadyExistsMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

                e.Name = name;
            }

            if (request.IsActive.HasValue)
                e.IsActive = request.IsActive.Value;

            await _uow.SaveChangesAsync(ct);

            var refreshed = await _uow.FacultyScopes.GetByIdWithRefsAsync(
                facultyScopeId,
                includeAssignments: false,
                ct);

            return ServiceResult<FacultyScopeResponseDTO>.Ok(await MapAsync(refreshed!, includeAssignments: false, ct));
        }

        public async Task<ServiceResult<FacultyScopeResponseDTO>> SetFacultiesAsync(
            int facultyScopeId,
            UnifiedSetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var scope = await _uow.FacultyScopes.QueryWithRefs(false, asNoTracking: false)
                .FirstOrDefaultAsync(x => x.FacultyScopeId == facultyScopeId, ct);
            if (scope is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var resolved = await UnifiedAcademicReferencePreparation.FacultiesAsync(_uow, request.ExternalFacultyIds, ct);
            if (!resolved.Success) return UnifiedAcademicReferencePreparation.Relay<FacultyScopeResponseDTO, List<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>>(resolved);
            var target = resolved.Data!.Select(f => f.FacultyId).ToList();
            // Use tracked rows so reactivation/deactivation is persisted.
            var existing = scope.Faculties?.ToList() ?? [];

            var byId = existing.ToDictionary(x => x.FacultyId);

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

            return ServiceResult<FacultyScopeResponseDTO>.Ok(await MapAsync(refreshed!, includeAssignments: false, ct));
        }

        public async Task<ServiceResult<bool>> AssignScopeToUserAsync(
            int facultyScopeId,
            AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (facultyScopeId <= 0)
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request is null)
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.GroupService_InstitutionalEmailRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.GroupService_DocumentRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var scope = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
                if (scope is null)
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var registerDto = new RegisterRequest
                {
                    Email = email,
                    Username = document,
                    Password = TemporaryPassword,
                    AspUserId = request.AspUserId,
                    Role = AppRoles.User
                };

                var known = await _identity.ResolveAsync(registerDto, ct);
                if (!known.Success) return UnifiedAcademicReferencePreparation.Relay<bool, int?>(known);
                // Assignment can be loaded without mutation before provisioning.
                if (known.Data.HasValue)
                    await _uow.UserFacultyScopeAssignments.GetByIdAsync(new object[] { known.Data.Value, facultyScopeId }, ct);
                var ensureResult = await _identity.EnsureAsync(registerDto, ct);
                if (!ensureResult.Success)
                {
                    return UnifiedAcademicReferencePreparation.Relay<bool, int>(ensureResult);
                }

                var appUserPk = ensureResult.Data;

                var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserPk }, ct);
                if (appUser is null)
                    return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_UnableToResolveAppUserMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

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
                return ServiceResult<bool>.Ok(true, ErrorMessages.UnifiedLegacy.FacultyScopeService_ScopeAssignedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<bool>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<bool>> UnassignScopeFromUserAsync(
            int facultyScopeId,
            int identityUserId,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            if (identityUserId <= 0)
                return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_IdentityUserIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var key = new object[] { identityUserId, facultyScopeId };
            var existing = await _uow.UserFacultyScopeAssignments.GetByIdAsync(key, ct);

            if (existing is null)
                return ServiceResult<bool>.Ok(true, ErrorMessages.UnifiedLegacy.FacultyScopeService_AssignmentNotFoundMessage);

            existing.IsActive = false;
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true, ErrorMessages.UnifiedLegacy.FacultyScopeService_ScopeUnassignedMessage);
        }

        public async Task<ServiceResult<IReadOnlyList<int>>> GetAllowedFacultyIdsForUserAsync(
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<IReadOnlyList<int>>.Fail(
                        ErrorMessages.UnifiedLegacy.FacultyScopeService_UserNotFoundMessage,
                        ErrorType.NotFound, ErrorCodes.Common.NotFound);
                }

                var ids = await _uow.UserFacultyScopeAssignments
                    .GetActiveFacultyIdsByUserAsync(actorUserId.Value, ct);

                return ServiceResult<IReadOnlyList<int>>.Ok(ids);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<IReadOnlyList<int>>.Fail(
                    ErrorMessages.UnifiedLegacy.FacultyScopeService_UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    ErrorCodes.Auth.UserNotAuthenticated);
            }
        }

        private const string TemporaryPassword = "Temp123*";









        // ========================= Helpers =========================

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

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

        private async Task<FacultyScopeResponseDTO> MapAsync(FacultyScope e, bool includeAssignments, CancellationToken ct)
        {
            var dto = new FacultyScopeResponseDTO
            {
                FacultyScopeId = e.FacultyScopeId,
                Name = e.Name,
                IsActive = e.IsActive,
                Faculties = e.Faculties?
                    .Select(f => new FacultyScopeFacultyItemDTO
                    {
                        FacultyId = f.FacultyId,
                        IsActive = f.IsActive
                    })
                    .OrderBy(x => x.FacultyId)
                    .ToList() ?? new List<FacultyScopeFacultyItemDTO>()
            };

            foreach (var item in dto.Faculties)
                item.ExternalFacultyId = (await _uow.Faculties.GetByIdAsync(new object[] { item.FacultyId }, ct))?.ExternalFacultyId;

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
