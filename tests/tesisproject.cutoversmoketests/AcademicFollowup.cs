using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;

internal static class AcademicFollowup
{
    public static async Task RunAsync(IServiceProvider services, string output, bool fullOnly = false)
    {
        Directory.CreateDirectory(output);
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<UnifiedDideDbContext>();
        UnifiedDideDbContextFactory.ValidateDestination(db.Database.GetConnectionString()!, []);
        if (db.Database.GetDbConnection().Database != "tesis_unified") throw new InvalidOperationException("Unexpected runtime database");
        var marker = "CA-" + Guid.NewGuid().ToString("N")[..7];
        var username = marker.Replace("-", "");
        var email = username + "@cutover.invalid";
        var aspId = RandomNumberGenerator.GetInt32(1500000000, 2000000000);
        var users = sp.GetRequiredService<UserManager<IdentityUser<int>>>();
        var results = new List<object>();
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri?.IsLoopback == true
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7015"), Timeout = TimeSpan.FromSeconds(60) };
        async Task<JsonElement?> Call(string name, HttpMethod method, string path, object? body = null, int expected = 200, bool envelope = true)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
            var metadata = new[] { "success", "data", "error", "errorCode", "message", "validationErrors" }.All(p => json.TryGetProperty(p, out _));
            var success = (int)response.StatusCode == expected && metadata && json.GetProperty("success").GetBoolean();
            results.Add(new { name, method = method.Method, path, status = (int)response.StatusCode, metadata, success,
                errorCode = json.TryGetProperty("errorCode", out var code) ? code.ToString() : null });
            await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"{name}: {(success ? "PASS" : "FAIL")} HTTP {(int)response.StatusCode} metadata={metadata}");
            if (!success) throw new InvalidOperationException($"{name}: {json.GetProperty("errorCode")} / {json.GetProperty("message")}");
            return json;
        }
        try
        {
            // Test authorization setup only; do not repeat the already-passed Auth HTTP suite.
            var actor = await sp.GetRequiredService<IUnifiedIdentityProvisioningService>().EnsureAsync(new()
            {
                Email = email, Username = username, AspUserId = aspId, Role = "user",
                Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA9!"
            });
            if (!actor.Success) throw new InvalidOperationException("Actor setup failed: " + actor.ErrorCode);
            var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException("Actor missing");
            if (!(await users.AddToRoleAsync(user, "superadmin")).Succeeded) throw new InvalidOperationException("Actor role setup failed");
            var token = sp.GetRequiredService<ITokenService>().CreateAccessToken(user.Id, email, ["superadmin"]);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.token);
            if (!fullOnly)
            {
            await Call("Faculty sync", HttpMethod.Post, "/api/catalog-sync/faculties");
            await Call("AcademicTerm sync", HttpMethod.Post, "/api/catalog-sync/academic-terms");
            await Call("Faculty roots", HttpMethod.Get, "/api/faculties/roots");
            await Call("Faculty hierarchy", HttpMethod.Get, "/api/faculties/hierarchy");
            await Call("Academic periods", HttpMethod.Get, "/api/academicperiods");
            }
            var faculty = await db.Faculties.AsNoTracking().FirstAsync(f => f.ExternalFacultyId != null && f.ParentFacultyId == null);
            var groupId = await db.Groups.Where(g => g.Name.StartsWith("CT-")).Select(g => g.GroupId).FirstAsync();
            var summary = new
            {
                roots = await db.Faculties.CountAsync(f => f.ParentFacultyId == null),
                children = await db.Faculties.CountAsync(f => f.ParentFacultyId != null),
                externalFacultyIds = await db.Faculties.CountAsync(f => f.ExternalFacultyId != null),
                terms = await db.AcademicTerms.CountAsync(),
                termsWithExternalIdAndDates = await db.AcademicTerms.CountAsync(t => t.ExternalPeriodId != null && t.StartDate != null && t.EndDate >= t.StartDate)
            };
            await File.WriteAllTextAsync(Path.Combine(output, "catalogs.json"), JsonSerializer.Serialize(summary));
            Console.WriteLine("Catalogs: " + JsonSerializer.Serialize(summary));
            await AcademicSmoke.RunAsync(sp, marker, username, email, aspId, groupId, faculty.ExternalFacultyId!.Value, Call, academicOnly: true, fullOnly: fullOnly);
            Console.WriteLine(fullOnly ? "CREATEFULL FOLLOWUP PASS" : "ACADEMIC FOLLOWUP PASS");
        }
        finally
        {
            var user = await users.FindByEmailAsync(email);
            if (user is not null)
            {
                await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user));
                await users.SetLockoutEnabledAsync(user, true);
                await users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await users.UpdateSecurityStampAsync(user);
            }
            await File.WriteAllTextAsync(Path.Combine(output, "fixture-marker.txt"), marker);
        }
    }
}
