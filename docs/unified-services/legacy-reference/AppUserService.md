# AppUser: lectura separada de Identity

GetAppUserIdByLocalIdAsync se migra: solo consulta AppUsers.GetByLocalIdAsync. No iguala IdLocal a IdUser.

## TODO UNIFIED: EnsureAppUserAsync / EnsureAppUsersAsync / EnsureSingleInternalAsync

Requieren UserManager, roles e Identity runtime. Each EnsureSingleInternalAsync confirma la UoW legacy; una lista puede quedar persistida parcialmente. Código exacto legacy:

```csharp
        public async Task<ServiceResult<int>> EnsureAppUserAsync(
            RegisterRequest dto,
            CancellationToken ct = default)
        {
            try
            {
                var idUser = await EnsureSingleInternalAsync(dto, ct);
                return ServiceResult<int>.Ok(idUser, AppUserEnsuredMessage);
            }
            catch (InvalidOperationException invEx)
            {
                return ServiceResult<int>.Fail(
                    invEx.Message,
                    ErrorType.Validation,
                    ErrorCodes.Common.InvalidRequest);
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResult<int>.Fail(
                    msg,
                    ErrorType.Conflict,
                    ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail(
                    ex.Message,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<List<int>>> EnsureAppUsersAsync(
            IEnumerable<RegisterRequest> dtos,
            CancellationToken ct = default)
        {
            try
            {
                var result = new List<int>();

                foreach (var dto in dtos)
                {
                    var idUser = await EnsureSingleInternalAsync(dto, ct);
                    result.Add(idUser);
                }

                return ServiceResult<List<int>>.Ok(result, AppUsersEnsuredMessage);
            }
            catch (InvalidOperationException invEx)
            {
                return ServiceResult<List<int>>.Fail(
                    invEx.Message,
                    ErrorType.Validation,
                    ErrorCodes.Common.InvalidRequest);
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                return ServiceResult<List<int>>.Fail(
                    msg,
                    ErrorType.Conflict,
                    ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<int>>.Fail(
                    ex.Message,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.UnexpectedError);
            }
        }
        private async Task<int> EnsureSingleInternalAsync(RegisterRequest dto, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            var userWasCreated = false;

            if (user is null)
            {
                user = new IdentityUser<int>
                {
                    Email = dto.Email,
                    UserName = dto.Username,
                    EmailConfirmed = false
                };

                var create = await _userManager.CreateAsync(user, dto.Password);
                if (!create.Succeeded)
                {
                    var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    throw new InvalidOperationException(msg);
                }

                userWasCreated = true;
            }

            try
            {
                await _userRoles.AssignRoleAsync(user, dto.Role, ct);
            }
            catch
            {
                if (userWasCreated)
                    await _userManager.DeleteAsync(user);

                throw;
            }

            AppUser? appUser = null;

            if (dto.AspUserId.HasValue)
                appUser = await _appUsers.GetByAspIdAsync(dto.AspUserId.Value, ct);

            if (appUser is null)
                appUser = await _appUsers.GetByLocalIdAsync(user.Id, ct);

            if (appUser is null)
            {
                appUser = new AppUser
                {
                    IdLocal = user.Id,
                    IdAsp = dto.AspUserId
                };

                await _appUsers.AddAsync(appUser, ct);
            }
            else
            {
                var modified = false;

                if (appUser.IdLocal is null)
                {
                    appUser.IdLocal = user.Id;
                    modified = true;
                }

                if (dto.AspUserId.HasValue && appUser.IdAsp is null)
                {
                    appUser.IdAsp = dto.AspUserId.Value;
                    modified = true;
                }

                if (modified)
                    _appUsers.Update(appUser);
            }

            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch
            {
                if (userWasCreated)
                    await _userManager.DeleteAsync(user);

                throw;
            }

            return appUser.IdUser;
        }
```
