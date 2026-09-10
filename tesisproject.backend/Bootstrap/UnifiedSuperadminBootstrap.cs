using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Bootstrap;

/// <summary>Operator-controlled startup only. Public registration retains its restricted role policy.</summary>
public static class UnifiedSuperadminBootstrap
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        var section = configuration.GetSection("BootstrapSuperadmin");
        if (!section.GetValue<bool>("Enabled")) return;

        var request = new RegisterRequest
        {
            AspUserId = section.GetValue<int?>("AspUserId"),
            Email = section["Email"]?.Trim() ?? "",
            Username = section["Username"]?.Trim() ?? "",
            Password = section["Password"] ?? "",
            Role = "user"
        };
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validation, true) ||
            request.AspUserId is not > 0)
            throw new InvalidOperationException("BootstrapSuperadmin requires a positive institutional AspUserId, valid Email, Username (max 10 characters) and Password matching the existing registration/Identity policy.");

        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var uow = provider.GetRequiredService<IUnifiedUnitOfWork>();
        var users = provider.GetRequiredService<UserManager<IdentityUser<int>>>();
        var identity = provider.GetRequiredService<IUnifiedIdentityProvisioningService>();

        // For privileged bootstrap, refuse stale profile configuration before any provisioning.
        var existing = await uow.AppUsers.GetByAspIdAsync(request.AspUserId.Value, ct);
        if (existing?.IdLocal is int existingLocalId)
        {
            var linked = await users.FindByIdAsync(existingLocalId.ToString());
            if (linked is null || !string.Equals(users.NormalizeEmail(linked.Email), users.NormalizeEmail(request.Email), StringComparison.Ordinal))
                throw new InvalidOperationException("BootstrapSuperadmin identity mapping/profile conflict. No role granted.");
        }

        // Keep the approved boundary's IdAsp/IdLocal checks and pending-change protection.
        var provisioned = await identity.EnsureAsync(request, ct);
        if (!provisioned.Success)
            throw new InvalidOperationException($"BootstrapSuperadmin provisioning failed ({provisioned.ErrorCode}). No role granted.");

        var bridge = await uow.AppUsers.GetByIdUserAsync(provisioned.Data, ct);
        if (bridge?.IdAsp != request.AspUserId || bridge.IdLocal is not int localId)
            throw new InvalidOperationException("BootstrapSuperadmin requires a valid Unified AppUser mapping.");
        var user = await users.FindByIdAsync(localId.ToString());
        if (user is null || !string.Equals(users.NormalizeEmail(user.Email), users.NormalizeEmail(request.Email), StringComparison.Ordinal))
            throw new InvalidOperationException("BootstrapSuperadmin identity mapping/profile conflict. No role granted.");

        if (!await users.IsInRoleAsync(user, "superadmin"))
        {
            var assignment = await users.AddToRoleAsync(user, "superadmin");
            if (!assignment.Succeeded)
                throw new InvalidOperationException("BootstrapSuperadmin role assignment failed: " + string.Join(", ", assignment.Errors.Select(e => e.Code)));
        }
        // Existing passwords are never reset; completed provisioning survives a role-assignment failure.
        provider.GetRequiredService<ILoggerFactory>().CreateLogger("UnifiedSuperadminBootstrap")
            .LogInformation("Unified superadmin bootstrap completed for AppUserId={AppUserId}, IdLocal={IdLocal}. Sign in again to obtain updated roles.", bridge.IdUser, localId);
    }
}
