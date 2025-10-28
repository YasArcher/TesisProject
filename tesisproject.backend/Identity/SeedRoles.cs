using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Identity;
public static class SeedRoles
{
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in new[] { "Admin", "Analyst" })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new ApplicationRole { Name = role });
        }

        var adminEmail = "admin@uta.edu.ec";
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Administrador" };
            await userMgr.CreateAsync(admin, "Admin#2025");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}