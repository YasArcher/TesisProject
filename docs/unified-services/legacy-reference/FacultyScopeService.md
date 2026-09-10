# Referencia legacy — FacultyScopeService

## TODO UNIFIED: AssignScopeToUserAsync

EnsureAppUserAsync depende de Identity y confirma anticipadamente. Separar provisión de identidad de asignación Unified.

Referencia exacta: `tesisproject.backend/Services/Implementations/FacultyScopeService.cs:229-309`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<bool>> AssignScopeToUserAsync(
            int facultyScopeId,
            AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (facultyScopeId <= 0)
                    return ServiceResult<bool>.Fail(FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request is null)
                    return ServiceResult<bool>.Fail(RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<bool>.Fail(InstitutionalEmailRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<bool>.Fail(DocumentRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var scope = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
                if (scope is null)
                    return ServiceResult<bool>.Fail(FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

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
                        ensureResult.Message ?? FailedToEnsureAppUserMessage,
                        ensureResult.Error);
                }

                var appUserPk = ensureResult.Data;

                var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserPk }, ct);
                if (appUser is null)
                    return ServiceResult<bool>.Fail(UnableToResolveAppUserMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

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
                return ServiceResult<bool>.Ok(true, ScopeAssignedMessage);
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
```

## TODO UNIFIED: CreateAsync

FacultyIds del contrato legacy provienen del catálogo externo. Definir resolución a FacultyId local antes de persistir.

Referencia exacta: `tesisproject.backend/Services/Implementations/FacultyScopeService.cs:75-124`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<FacultyScopeResponseDTO>> CreateAsync(
            CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FacultyScopeResponseDTO>.Fail(NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var exists = await _uow.FacultyScopes.ExistsAsync(x => x.Name == name, ct);
            if (exists)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(ScopeNameAlreadyExistsMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

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
            await _uow.SaveChangesAsync(ct);

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
```

## TODO UNIFIED: SetFacultiesAsync

FacultyIds del contrato legacy provienen del catálogo externo. Definir resolución a FacultyId local antes de persistir.

Referencia exacta: `tesisproject.backend/Services/Implementations/FacultyScopeService.cs:167-227`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<FacultyScopeResponseDTO>> SetFacultiesAsync(
            int facultyScopeId,
            SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
        {
            if (facultyScopeId <= 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(FacultyScopeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            if (request is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var scope = await _uow.FacultyScopes.GetByIdAsync(new object[] { facultyScopeId }, ct);
            if (scope is null)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(FacultyScopeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var normalized = NormalizeFacultyIds(request.FacultyIds);
            if (normalized.Invalid.Count > 0)
                return ServiceResult<FacultyScopeResponseDTO>.Fail(
                    $"Invalid FacultyIds: {string.Join(", ", normalized.Invalid)}",
                    ErrorType.Validation);

            var target = normalized.Ids;

            var existing = await _uow.FacultyScopeFaculties
                .GetAllAsync(x => x.FacultyScopeId == facultyScopeId, ct);

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

            return ServiceResult<FacultyScopeResponseDTO>.Ok(Map(refreshed!, includeAssignments: false));
        }
```
