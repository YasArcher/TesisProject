using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;

namespace tesisproject.backend.Services.Unified;

// Deliberately not called by Program.cs: current controllers still use legacy services.
// Future composition must register one scoped UnifiedDideDbContext and its Unified UoW
// before using this extension; never combine it with the legacy Identity registration.
public static class UnifiedIdentityRegistration
{
    public static IdentityBuilder AddUnifiedIdentityBoundary(this IServiceCollection services)
    {
        services.AddScoped<IUnifiedIdentityProvisioningService, UnifiedIdentityProvisioningService>();
        services.AddScoped<IUnifiedIdentityQueryService, UnifiedIdentityQueryService>();
        services.AddScoped<IUnifiedAppUserService, UnifiedAppUserService>();
        return services.AddIdentityCore<IdentityUser<int>>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        }).AddRoles<IdentityRole<int>>()
          .AddEntityFrameworkStores<UnifiedDideDbContext>()
          .AddSignInManager<SignInManager<IdentityUser<int>>>()
          .AddDefaultTokenProviders();
    }
}
