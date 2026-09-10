using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedIdentityProvisioningService(
    UnifiedDideDbContext context, IUnifiedUnitOfWork uow,
    UserManager<IdentityUser<int>> users, RoleManager<IdentityRole<int>> roles,
    ILogger<UnifiedIdentityProvisioningService> logger) : IUnifiedIdentityProvisioningService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        { "user", "technical", "financial", "coordinador" };

    private sealed record Resolution(AppUser? Bridge, IdentityUser<int>? User);

    public async Task<ServiceResult<int?>> ResolveAsync(RegisterRequest request, CancellationToken ct = default)
    {
        try
        {
        var result = await ResolveInternalAsync(request, ct);
        return result.Success ? ServiceResult<int?>.Ok(result.Data!.Bridge?.IdUser)
            : UnifiedAcademicReferencePreparation.Relay<int?, Resolution>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity resolution failed for IdAsp={IdAsp}", request?.AspUserId);
            return ServiceResult<int?>.Fail(ErrorMessages.IdentityProvisioning.OperationFailed, ErrorType.Unexpected, ErrorCodes.IdentityProvisioning.OperationFailed);
        }
    }

    private async Task<ServiceResult<Resolution>> ResolveInternalAsync(RegisterRequest request, CancellationToken ct)
    {
        // These entry points provision institutional participants. Missing IdAsp is rejected;
        // no local-only institutional identity is inferred from an email address.
        if (request is null)
            return ServiceResult<Resolution>.Fail(ErrorMessages.Common.RequestRequired, ErrorType.Validation, ErrorCodes.Common.RequestRequired);
        var invalid = new Dictionary<string, string[]>();
        if (request.AspUserId is not > 0) invalid[nameof(RegisterRequest.AspUserId)] = [ErrorMessages.Common.InvalidId];
        if (string.IsNullOrWhiteSpace(request.Email)) invalid[nameof(RegisterRequest.Email)] = [ErrorMessages.Common.InvalidRequest];
        if (string.IsNullOrWhiteSpace(request.Username)) invalid[nameof(RegisterRequest.Username)] = [ErrorMessages.Common.InvalidRequest];
        if (invalid.Count > 0)
            return ServiceResult<Resolution>.Fail(ErrorMessages.Common.InvalidRequest, ErrorType.Validation, ErrorCodes.Common.InvalidRequest,
                invalid);

        var bridge = await uow.AppUsers.GetByAspIdAsync(request.AspUserId!.Value, ct);
        var byEmail = await users.FindByEmailAsync(request.Email.Trim());
        // IdAsp is the canonical identifier for an institutional user already known by DIDE.
        // Email and other profile data come from the external institutional source and may change
        // or arrive inconsistent. They must not silently relink an existing AppUser to another
        // local Identity account. If IdAsp resolves to an AppUser whose IdLocal conflicts with
        // the Identity resolved from the incoming data, the operation must fail explicitly.
        if (bridge?.IdLocal is int localId)
        {
            var linked = await users.FindByIdAsync(localId.ToString());
            if (linked is null || (byEmail is not null && byEmail.Id != localId)) return Conflict(request, bridge, byEmail);
            return ServiceResult<Resolution>.Ok(new(bridge, linked));
        }
        if (byEmail is not null)
        {
            var byLocal = await uow.AppUsers.GetByLocalIdAsync(byEmail.Id, ct);
            if (byLocal is not null && ((bridge is not null && byLocal.IdUser != bridge.IdUser) ||
                (byLocal.IdAsp.HasValue && byLocal.IdAsp != request.AspUserId)))
                return Conflict(request, byLocal, byEmail);
            bridge ??= byLocal;
        }
        return ServiceResult<Resolution>.Ok(new(bridge, byEmail));
    }

    private ServiceResult<Resolution> Conflict(RegisterRequest request, AppUser bridge, IdentityUser<int>? identity)
    {
        logger.LogWarning("Institutional identity conflict: IdAsp={IdAsp}, IdUser={IdUser}, IdLocal={IdLocal}, ResolvedIdentityId={ResolvedIdentityId}",
            request.AspUserId, bridge.IdUser, bridge.IdLocal, identity?.Id);
        return ServiceResult<Resolution>.Fail(ErrorMessages.IdentityProvisioning.MappingConflict, ErrorType.Conflict, ErrorCodes.IdentityProvisioning.MappingConflict);
    }

    public async Task<ServiceResult<int>> EnsureAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // One DI-owned context, two sequential owners. Never flush a caller's pending aggregate.
        if (context.ChangeTracker.HasChanges())
            return ServiceResult<int>.Fail(ErrorMessages.IdentityProvisioning.PendingChanges, ErrorType.Conflict, ErrorCodes.IdentityProvisioning.PendingChanges);
        IdentityUser<int>? created = null;
        AppUser? bridge = null;
        int? oldLocal = null, oldAsp = null;
        var bridgeAdded = false;
        try
        {
            var resolved = await ResolveInternalAsync(request, ct);
            if (!resolved.Success) return UnifiedAcademicReferencePreparation.Relay<int, Resolution>(resolved);
            var role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role.Trim();
            if (!AllowedRoles.Contains(role) || !await roles.RoleExistsAsync(role))
                return ServiceResult<int>.Fail(ErrorMessages.IdentityProvisioning.InvalidRole, ErrorType.Validation, ErrorCodes.IdentityProvisioning.InvalidRole);
            var user = resolved.Data!.User;
            if (user is null)
            {
                user = new IdentityUser<int> { Email = request.Email.Trim(), UserName = request.Username.Trim(), EmailConfirmed = false };
                var creation = await users.CreateAsync(user, request.Password);
                if (!creation.Succeeded) return IdentityFailure(creation);
                created = user;
            }
            if (!await users.IsInRoleAsync(user, role))
            {
                var assignment = await users.AddToRoleAsync(user, role);
                if (!assignment.Succeeded) throw new InvalidOperationException(ErrorMessages.IdentityProvisioning.OperationFailed);
            }
            bridge = resolved.Data.Bridge;
            if (bridge is null)
            {
                bridge = new AppUser { IdLocal = user.Id, IdAsp = request.AspUserId };
                bridgeAdded = true;
                await uow.AppUsers.AddAsync(bridge, ct);
            }
            else
            {
                oldLocal = bridge.IdLocal; oldAsp = bridge.IdAsp;
                bridge.IdLocal ??= user.Id;
                bridge.IdAsp ??= request.AspUserId;
                if (oldLocal != bridge.IdLocal || oldAsp != bridge.IdAsp) uow.AppUsers.Update(bridge);
            }
            if (context.ChangeTracker.HasChanges()) await uow.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(bridge.IdUser);
        }
        catch (Exception ex)
        {
            // Compensation belongs only to this incomplete provisioning, never to Project.
            if (bridge is not null)
            {
                context.Entry(bridge).State = EntityState.Detached;
                if (!bridgeAdded) { bridge.IdLocal = oldLocal; bridge.IdAsp = oldAsp; }
            }
            if (created is not null)
            {
                try
                {
                    foreach (var entry in context.ChangeTracker.Entries<IdentityUserRole<int>>()
                        .Where(e => e.Entity.UserId == created.Id && e.State == EntityState.Added).ToList())
                        entry.State = EntityState.Detached;
                    // Unified uses NoAction FKs, including Identity roles. Remove newly created
                    // account memberships explicitly before deleting this incomplete account.
                    var memberships = await users.GetRolesAsync(created);
                    if (memberships.Count > 0) await users.RemoveFromRolesAsync(created, memberships);
                    var deletion = await users.DeleteAsync(created);
                    if (!deletion.Succeeded) logger.LogError("Incomplete Identity cleanup failed for IdLocal={IdLocal}", created.Id);
                }
                catch (Exception cleanup) { logger.LogError(cleanup, "Incomplete Identity cleanup failed for IdLocal={IdLocal}", created.Id); }
            }
            logger.LogError(ex, "Identity provisioning failed for IdAsp={IdAsp}", request?.AspUserId);
            return ServiceResult<int>.Fail(ErrorMessages.IdentityProvisioning.OperationFailed,
                ex is DbUpdateException ? ErrorType.Conflict : ErrorType.Unexpected, ErrorCodes.IdentityProvisioning.OperationFailed);
        }
    }

    private static ServiceResult<int> IdentityFailure(IdentityResult result) => ServiceResult<int>.Fail(
        ErrorMessages.IdentityProvisioning.OperationFailed, ErrorType.Validation, ErrorCodes.IdentityProvisioning.OperationFailed,
        new Dictionary<string, string[]> { ["Identity"] = result.Errors.Select(e => e.Code).ToArray() });

    public async Task<ServiceResult<List<int>>> EnsureSelectedAsync(IEnumerable<RegisterRequest> requests, CancellationToken ct = default)
    {
        try
        {
        if (requests is null)
            return ServiceResult<List<int>>.Fail(ErrorMessages.Common.RequestRequired, ErrorType.Validation, ErrorCodes.Common.RequestRequired);
        if (context.ChangeTracker.HasChanges())
            return ServiceResult<List<int>>.Fail(ErrorMessages.IdentityProvisioning.PendingChanges, ErrorType.Conflict, ErrorCodes.IdentityProvisioning.PendingChanges);
        var selected = requests.ToList();
        // Resolve all contradictions and role errors before provisioning the first participant.
        foreach (var request in selected)
        {
            var result = await ResolveInternalAsync(request, ct);
            if (!result.Success) return UnifiedAcademicReferencePreparation.Relay<List<int>, Resolution>(result);
            var role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role.Trim();
            if (!AllowedRoles.Contains(role) || !await roles.RoleExistsAsync(role))
                return ServiceResult<List<int>>.Fail(ErrorMessages.IdentityProvisioning.InvalidRole, ErrorType.Validation, ErrorCodes.IdentityProvisioning.InvalidRole);
        }
        var ids = new List<int>();
        foreach (var request in selected)
        {
            var result = await EnsureAsync(request, ct);
            if (!result.Success) return UnifiedAcademicReferencePreparation.Relay<List<int>, int>(result);
            ids.Add(result.Data);
        }
        return ServiceResult<List<int>>.Ok(ids);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Selected Identity provisioning failed");
            return ServiceResult<List<int>>.Fail(ErrorMessages.IdentityProvisioning.OperationFailed, ErrorType.Unexpected, ErrorCodes.IdentityProvisioning.OperationFailed);
        }
    }
}
