using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using tesisproject.backend.Bootstrap;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;

internal static class SuperadminBootstrapTests
{
    public static async Task RunAsync()
    {
        var checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++; Console.WriteLine("PASS: " + message);
        }
        IConfiguration Settings(bool enabled, int? asp = 1001, string email = "bootstrap@example.test", string password = "bootstrap-test-pass") =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapSuperadmin:Enabled"] = enabled.ToString(),
                ["BootstrapSuperadmin:AspUserId"] = asp?.ToString(),
                ["BootstrapSuperadmin:Email"] = email,
                ["BootstrapSuperadmin:Username"] = "bootadmin",
                ["BootstrapSuperadmin:Password"] = password
            }).Build();
        using (var empty = new ServiceCollection().BuildServiceProvider())
        {
            await UnifiedSuperadminBootstrap.RunAsync(empty, Settings(false));
            Check(true, "Disabled bootstrap requires no database or credentials");
            try { await UnifiedSuperadminBootstrap.RunAsync(empty, Settings(true, null)); throw new Exception("Missing IdAsp accepted"); }
            catch (InvalidOperationException) { Check(true, "Missing institutional IdAsp rejected before persistence"); }
        }
        var database = "tesis_superadmin_test_" + Guid.NewGuid().ToString("N");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.Configuration["ExternalApis:BaseUrl"] = "https://directory.invalid/";
        builder.Configuration["Jwt:Key"] = new string('s', 64);
        builder.Configuration["Jwt:Issuer"] = "test";
        builder.Configuration["Jwt:Audience"] = "test";
        builder.Configuration["Cors:AllowedOrigins:0"] = "http://localhost:8090";
        builder.Configuration["Cors:AllowedOrigins:1"] = "http://localhost:8091";
        builder.Services.AddUnifiedDide(builder.Configuration, options => options.UseSqlServer(
            $@"Server=.\DINNOVA;Database={database};Integrated Security=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));
        typeof(UnifiedDideDbContext).Assembly.GetType("StartupExtensions")!.GetMethod("ConfigureCors")!.Invoke(null, [builder]);
        await using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var fixture = provider.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
        try
        {
            await db.Database.MigrateAsync();
            var roles = fixture.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            foreach (var role in new[] { "user", "superadmin" }) Check((await roles.CreateAsync(new(role))).Succeeded, "Seed role " + role);
            await UnifiedSuperadminBootstrap.RunAsync(provider, Settings(true));
            var users = fixture.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
            var user = (await users.FindByEmailAsync("bootstrap@example.test"))!;
            var bridge = await db.Set<AppUser>().SingleAsync(x => x.IdAsp == 1001);
            Check(bridge.IdLocal == user.Id && await users.IsInRoleAsync(user, "superadmin"), "New Identity -> AppUser with canonical IdAsp -> superadmin");
            await UnifiedSuperadminBootstrap.RunAsync(provider, Settings(true, password: "different-test-password"));
            Check(await db.Users.CountAsync() == 1 && await db.Set<AppUser>().CountAsync() == 1 &&
                await users.CheckPasswordAsync(user, "bootstrap-test-pass"), "Rerun reuses account without duplicates or password reset");

            try { await UnifiedSuperadminBootstrap.RunAsync(provider, Settings(true, 2002)); throw new Exception("Relink accepted"); }
            catch (InvalidOperationException) { Check(true, "Email attached to another IdAsp is rejected"); }
            try { await UnifiedSuperadminBootstrap.RunAsync(provider, Settings(true, email: "other@example.test")); throw new Exception("Wrong profile accepted"); }
            catch (InvalidOperationException) { Check(true, "Canonical account/profile mismatch rejected"); }
            Check(await db.Users.CountAsync() == 1 && await db.Set<AppUser>().CountAsync() == 1 &&
                (await db.Set<AppUser>().AsNoTracking().SingleAsync()).IdAsp == 1001, "Conflicts leave identity mapping unchanged");

            await using var ordinaryScope = provider.CreateAsyncScope();
            var ordinary = ordinaryScope.ServiceProvider.GetRequiredService<IUnifiedIdentityProvisioningService>();
            var denied = await ordinary.EnsureAsync(new RegisterRequest { AspUserId = 3003, Email = "public@example.test", Username = "public", Password = "test-password", Role = "superadmin" });
            Check(!denied.Success && !await db.Users.AnyAsync(x => x.Email == "public@example.test"), "Public provisioning still rejects superadmin");
            var auth = ordinaryScope.ServiceProvider.GetRequiredService<IUnifiedAuthService>();
            var login = await auth.LoginAsync(new LoginRequest { Email = user.Email!, Password = "bootstrap-test-pass" }, "127.0.0.1", default);
            Check(login.Result.Success && new JwtSecurityTokenHandler().ReadJwtToken(login.Result.Data!.AccessToken)
                .Claims.Any(c => c.Value == "superadmin" && c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase)), "Fresh login issues JWT containing superadmin");

            var existingResult = await ordinary.EnsureAsync(new RegisterRequest { AspUserId = 4004, Email = "existing@example.test", Username = "existing", Password = "existing-test-pass", Role = "user" });
            Check(existingResult.Success, "Existing ordinary account provisioned through Unified");
            var existingUser = (await users.FindByEmailAsync("existing@example.test"))!;
            Check(!await users.IsInRoleAsync(existingUser, "superadmin"), "Existing account starts without privileged role");
            await UnifiedSuperadminBootstrap.RunAsync(provider, Settings(true, 4004, "existing@example.test"));
            Check(await users.IsInRoleAsync(existingUser, "superadmin") && await users.CheckPasswordAsync(existingUser, "existing-test-pass") &&
                (await db.Set<AppUser>().AsNoTracking().SingleAsync(x => x.IdAsp == 4004)).IdUser == existingResult.Data,
                "Existing account promoted without relinking or password reset");

            var cors = fixture.ServiceProvider.GetRequiredService<ICorsService>();
            var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy("AllowFrontend")!;
            foreach (var origin in new[] { "http://localhost:8090", "http://localhost:8091", "http://unapproved.invalid" })
            {
                var http = new DefaultHttpContext(); http.Request.Headers.Origin = origin;
                Check(cors.EvaluatePolicy(http, policy).IsOriginAllowed == !origin.Contains("unapproved"), "CORS origin " + origin);
            }
        }
        finally { await db.Database.EnsureDeletedAsync(); }
        Console.WriteLine($"PASS: {checks} superadmin bootstrap / Identity / JWT / CORS checks.");
    }
}
