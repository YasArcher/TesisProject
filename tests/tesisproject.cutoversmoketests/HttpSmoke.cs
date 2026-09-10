using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.shared.DTOs.Auth;

internal static class HttpSmoke
{
    public static async Task RunAsync(IServiceProvider services, string output, bool skipExternal)
    {
        var results = new List<object>();
        var marker = "CT-" + Guid.NewGuid().ToString("N")[..7];
        var username = marker.Replace("-", "");
        var email = username + "@cutover.invalid";
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA9!";
        var aspId = RandomNumberGenerator.GetInt32(1500000000, 2000000000);
        using var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            // Only the loopback Development backend is used; no external TLS validation is changed.
            ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri?.IsLoopback == true
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7015"), Timeout = TimeSpan.FromSeconds(45) };
        async Task<JsonElement?> Call(string name, HttpMethod method, string path, object? body = null, int expected = 200, bool envelope = true)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            JsonElement? json = string.IsNullOrEmpty(text) ? null : JsonDocument.Parse(text).RootElement.Clone();
            var metadata = !envelope || (json.HasValue && new[] { "success", "data", "error", "errorCode", "message", "validationErrors" }.All(p => json.Value.TryGetProperty(p, out _)));
            var success = (int)response.StatusCode == expected && metadata &&
                (!envelope || expected != 200 || json!.Value.GetProperty("success").GetBoolean());
            var code = json.HasValue && json.Value.TryGetProperty("errorCode", out var errorCode) ? errorCode.ToString() : null;
            results.Add(new { name, method = method.Method, path, status = (int)response.StatusCode, expected, metadata, success, errorCode = code });
            Console.WriteLine($"{name}: {(success ? "PASS" : "FAIL")} HTTP {(int)response.StatusCode} metadata={metadata} code={code}");
            await File.WriteAllTextAsync(Path.Combine(output, "http-smoke.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            return json;
        }
        try
        {
            await Call("Health", HttpMethod.Get, "/health", envelope: false);
            await Call("Swagger", HttpMethod.Get, "/swagger/v1/swagger.json", envelope: false);
            await Call("Projects requires authentication", HttpMethod.Get, "/api/projects", expected: 401, envelope: false);
            await Call("Auth register", HttpMethod.Post, "/api/auth/register", new RegisterRequest
            { Email = email, Username = username, Password = password, AspUserId = aspId, Role = "user" });
            // Registration deliberately cannot grant superadmin. Elevate only this freshly generated local fixture via Identity.
            await using (var setup = services.CreateAsyncScope())
            {
                var manager = setup.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
                var fixture = await manager.FindByEmailAsync(email) ?? throw new InvalidOperationException("Fixture registration failed");
                if (!(await manager.AddToRoleAsync(fixture, "superadmin")).Succeeded)
                    throw new InvalidOperationException("Fixture role assignment failed");
            }
            var login = await Call("Auth login", HttpMethod.Post, "/api/auth/login", new LoginRequest { Email = email, Password = password });
            if (login?.GetProperty("success").GetBoolean() != true) return;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Value.GetProperty("data").GetProperty("accessToken").GetString());
            var refresh = await Call("Auth refresh", HttpMethod.Post, "/api/auth/refresh");
            if (refresh?.GetProperty("success").GetBoolean() == true)
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refresh.Value.GetProperty("data").GetProperty("accessToken").GetString());
            if (!skipExternal)
            {
                await Call("Faculty sync", HttpMethod.Post, "/api/catalog-sync/faculties");
                await Call("AcademicTerm sync", HttpMethod.Post, "/api/catalog-sync/academic-terms");
                await Call("Academic periods provider", HttpMethod.Get, "/api/academicperiods");
            }
            else results.Add(new { name = "External synchronization / academic periods", status = "SKIPPED", reason = "Explicit operator deferral; not validated." });
            await Call("Faculty roots", HttpMethod.Get, "/api/faculties/roots");
            await Call("Faculty hierarchy", HttpMethod.Get, "/api/faculties/hierarchy");
            await using (var counts = services.CreateAsyncScope())
            {
                var current = counts.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
                await Call("Projects list", HttpMethod.Get, "/api/projects", expected: await current.Projects.AnyAsync() ? 200 : 404);
                await Call("Products list", HttpMethod.Get, "/api/products", expected: await current.Products.AnyAsync() ? 200 : 404);
            }
            await Call("Projects missing get", HttpMethod.Get, "/api/projects/2147483647", expected: 404);
            await Call("FacultyScope list", HttpMethod.Get, "/api/facultyscopes");
            await Call("Document missing get", HttpMethod.Get, "/api/documents/2147483647", expected: 404);
            await Call("Objectives list", HttpMethod.Get, "/api/projectobjectives/by-project/2147483647");
            await Call("Visits list", HttpMethod.Get, "/api/visits", expected: 404);

            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
            if (db.Database.GetDbConnection().Database != "tesis_unified") throw new InvalidOperationException("Unexpected fixture database");
            // Minimal explicit local fixture catalogs; never source Faculty/AcademicTerm from integration databases.
            await db.Database.ExecuteSqlRawAsync("""
                SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
                IF NOT EXISTS(SELECT 1 FROM dbo.GroupTypes WHERE Id=1)
                BEGIN SET IDENTITY_INSERT dbo.GroupTypes ON; INSERT dbo.GroupTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Integrantes',1,1); SET IDENTITY_INSERT dbo.GroupTypes OFF; END;
                IF NOT EXISTS(SELECT 1 FROM dbo.MemberRoleTypes WHERE Id=3)
                BEGIN SET IDENTITY_INSERT dbo.MemberRoleTypes ON; INSERT dbo.MemberRoleTypes(Id,Name,IsActive,IsLocked,Flag) VALUES(3,N'Investigador',1,1,1); SET IDENTITY_INSERT dbo.MemberRoleTypes OFF; END;
                IF NOT EXISTS(SELECT 1 FROM dbo.ProductTypes WHERE Id=1)
                BEGIN SET IDENTITY_INSERT dbo.ProductTypes ON; INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Producto smoke',1,0); SET IDENTITY_INSERT dbo.ProductTypes OFF; END;
                """);
            var actor = await db.AppUsers.SingleAsync(u => u.IdAsp == aspId);
            var group = await Call("Group create", HttpMethod.Post, "/api/groups", new { groupTypeId = 1, name = marker });
            var groupId = group!.Value.GetProperty("data").GetProperty("groupId").GetInt32();
            await Call("Group list", HttpMethod.Get, "/api/groups/type/1");
            await Call("Group AddMember", HttpMethod.Post, "/api/groups/members", new { groupId, memberRole = 3, email, document = username, aspUserId = aspId });
            var product = await Call("Product create", HttpMethod.Post, "/api/products", new { title = marker, productTypeId = 1, authorUserIds = new[] { actor.IdUser } });
            if (product?.GetProperty("success").GetBoolean() == true)
            {
                var productId = product.Value.GetProperty("data").GetProperty("id").GetInt32();
                await Call("Product read", HttpMethod.Get, $"/api/products/{productId}");
                await Call("Products list after create", HttpMethod.Get, "/api/products");
            }
            var faculty = await db.Faculties.AsNoTracking().FirstOrDefaultAsync(f => f.ExternalFacultyId != null && f.ParentFacultyId == null);
            if (skipExternal)
            {
                results.Add(new { name = "Project create / CreateFull / FacultyScope assign", status = "SKIPPED", reason = "Explicit operator deferral of live catalogs; no synthetic academic fallback." });
                Console.WriteLine("ACADEMIC FLOWS SKIPPED: explicitly deferred by operator.");
            }
            else if (faculty is null || !await db.AcademicTerms.AnyAsync())
            {
                results.Add(new { name = "Academic flows", success = false, blocker = "External provider unavailable: runtime Faculty/AcademicTerm synchronization required before Project create/CreateFull/FacultyScope assign." });
                Console.WriteLine("ACADEMIC FLOWS BLOCKED: synchronized runtime catalogs unavailable; no synthetic academic fallback used.");
            }
            else
            {
                await AcademicSmoke.RunAsync(scope.ServiceProvider, marker, username, email, aspId, groupId, faculty.ExternalFacultyId!.Value, Call);
            }
            await Call("Auth logout", HttpMethod.Post, "/api/auth/logout");
            await Call("Refresh after logout", HttpMethod.Post, "/api/auth/refresh", expected: 401);
        }
        finally
        {
            // Preserve small auditable domain fixtures while disabling the temporary privileged login.
            await using var scope = services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
            var user = await users.FindByEmailAsync(email);
            if (user is not null)
            {
                await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user));
                await users.SetLockoutEnabledAsync(user, true);
                await users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await users.UpdateSecurityStampAsync(user);
            }
            await File.WriteAllTextAsync(Path.Combine(output, "http-smoke.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(output, "fixture-marker.txt"), marker);
            var report = JsonSerializer.SerializeToElement(results);
            if (report.EnumerateArray().Any(r => r.TryGetProperty("success", out var success) && !success.GetBoolean()))
                Environment.ExitCode = 1;
        }
    }
}
