using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Document.Response;

internal static class AcademicSmoke
{
    internal delegate Task<JsonElement?> Call(string name, HttpMethod method, string path, object? body = null, int expected = 200, bool envelope = true);
    public static async Task RunAsync(IServiceProvider sp, string marker, string username, string email, int aspId, int groupId, int externalFacultyId, Call call, bool academicOnly = false, bool fullOnly = false)
    {
        var db = sp.GetRequiredService<UnifiedDideDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
            IF NOT EXISTS(SELECT 1 FROM dbo.MemberRoleTypes WHERE Id=1)
            BEGIN SET IDENTITY_INSERT dbo.MemberRoleTypes ON; INSERT dbo.MemberRoleTypes(Id,Name,IsActive,IsLocked,Flag) VALUES(1,N'Coordinador',1,1,1); SET IDENTITY_INSERT dbo.MemberRoleTypes OFF; END;
            IF NOT EXISTS(SELECT 1 FROM dbo.ProjectTypes WHERE Id=1)
            BEGIN SET IDENTITY_INSERT dbo.ProjectTypes ON; INSERT dbo.ProjectTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Investigación',1,1); SET IDENTITY_INSERT dbo.ProjectTypes OFF; END;
            IF NOT EXISTS(SELECT 1 FROM dbo.ProjectStates WHERE Id=3)
            BEGIN SET IDENTITY_INSERT dbo.ProjectStates ON; INSERT dbo.ProjectStates(Id,Name,IsActive,IsLocked) VALUES(3,N'En ejecución',1,1); SET IDENTITY_INSERT dbo.ProjectStates OFF; END;
            IF NOT EXISTS(SELECT 1 FROM dbo.ProjectOriginTypes WHERE Id=1)
            BEGIN SET IDENTITY_INSERT dbo.ProjectOriginTypes ON; INSERT dbo.ProjectOriginTypes(Id,Name,IsActive,IsLocked) VALUES(1,N'Interno',1,1); SET IDENTITY_INSERT dbo.ProjectOriginTypes OFF; END;
            IF NOT EXISTS(SELECT 1 FROM dbo.Convocations WHERE Id=1)
            BEGIN SET IDENTITY_INSERT dbo.Convocations ON; INSERT dbo.Convocations(Id,Code,Name,IsActive,IsLocked) VALUES(1,N'CT',N'Cutover smoke',1,0); SET IDENTITY_INSERT dbo.Convocations OFF; END;
            """);
        var term = await db.AcademicTerms.AsNoTracking().Where(t => t.StartDate != null).OrderByDescending(t => t.StartDate).FirstAsync();
        var projectRequest = new AddProjectRequestDTO
        {
            ProjectName = marker, ProjectTypeId = 1, ProjectGroupId = groupId, ProjectCode = marker,
            ApprovalDate = term.StartDate!.Value, StartDate = term.StartDate, DurationInMonths = 6,
            FacultyId = externalFacultyId, ProjectStateId = 3, ConvocationId = 1, ProjectOriginTypeId = 1
        };
        if (!fullOnly)
        {
        var project = await call("Project create", HttpMethod.Post, "/api/projects", projectRequest);
        if (project?.GetProperty("success").GetBoolean() == true)
        {
            var id = project.Value.GetProperty("data").GetProperty("projectId").GetInt32();
            await call("Project get", HttpMethod.Get, $"/api/projects/{id}");
            if (!academicOnly)
            {
                await call("Objectives by project", HttpMethod.Get, $"/api/projectobjectives/by-project/{id}");
                await call("Visits by project", HttpMethod.Get, $"/api/visits/by-project/{id}");
            }
            var stored = await db.Projects.AsNoTracking().SingleAsync(p => p.ProjectId == id);
            var local = await db.Faculties.AsNoTracking().SingleAsync(f => f.ExternalFacultyId == externalFacultyId);
            if (stored.FacultyId != local.FacultyId) throw new InvalidOperationException("Project Faculty FK is not local");
        }
        var facultyScope = await call("FacultyScope create", HttpMethod.Post, "/api/facultyscopes", new { name = marker, facultyIds = new[] { externalFacultyId } });
        if (facultyScope?.GetProperty("success").GetBoolean() == true)
        {
            var id = facultyScope.Value.GetProperty("data").GetProperty("facultyScopeId").GetInt32();
            await call("FacultyScope get", HttpMethod.Get, $"/api/facultyscopes/{id}");
            await call("FacultyScope assign", HttpMethod.Post, $"/api/facultyscopes/{id}/assign", new { email, document = username, aspUserId = aspId });
            var local = await db.Faculties.AsNoTracking().SingleAsync(f => f.ExternalFacultyId == externalFacultyId);
            var returned = facultyScope.Value.GetProperty("data").GetProperty("faculties")[0];
            if (returned.GetProperty("facultyId").GetInt32() != local.FacultyId || returned.GetProperty("externalFacultyId").GetInt32() != externalFacultyId)
                throw new InvalidOperationException("FacultyScope local/external semantic mismatch");
        }
        }
        // Bounded live provider lookup for one coordinator; no academic HTTP fallback or cache.
        var distributivos = await sp.GetRequiredService<IExternalDistributivosService>()
            .GetDistributivosAsync();
        if (!distributivos.Success || distributivos.Data is null)
            throw new InvalidOperationException("CreateFull fixture lookup: " + distributivos.ErrorCode + " / " + distributivos.Message);
        // Fixture discovery uses the provider's returned external ID, independent of its text search filter.
        distributivos.Data.RemoveAll(d => d.PeriodId != term.ExternalPeriodId);
        var candidateEmails = distributivos.Data.Where(d => d.AspId > 0 && d.CareerId > 0 && !string.IsNullOrWhiteSpace(d.Email))
            .Select(d => d.Email).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
        var directory = await sp.GetRequiredService<IExternalDirectoryClient>().GetByEmailsAsync(candidateEmails);
        if (!directory.Success || directory.Data is null)
            throw new InvalidOperationException("CreateFull blocked: live Directory unavailable");
        var profile = directory.Data.FirstOrDefault(p => p.AspId > 0 && p.Document.Length is > 0 and <= 10 &&
            p.Careers.Any(c => distributivos.Data.Any(d => d.Email.Equals(p.Email, StringComparison.OrdinalIgnoreCase) && d.CareerId == c.FacultyCareerId)));
        if (profile is null) throw new InvalidOperationException("CreateFull blocked: no eligible live coordinator fixture");
        var users = sp.GetRequiredService<UserManager<IdentityUser<int>>>();
        var existed = await users.FindByEmailAsync(profile.Email) is not null;
        if (existed) throw new InvalidOperationException("Choose an unused local coordinator identity for smoke; existing user will not be changed");
        try
        {
            await call("Coordinator fixture register", HttpMethod.Post, "/api/auth/register", new RegisterRequest
            {
                Email = profile.Email, Username = profile.Document, AspUserId = profile.AspId, Role = "coordinador",
                Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA9!"
            });
            projectRequest.ProjectName = marker + " Full";
            projectRequest.ProjectCode = marker;
            await call("CreateFull", HttpMethod.Post, "/api/projects/full", new AddProjectFullRequestDTO
            {
                Project = projectRequest, ProjectDocumentData = new DocumentResponseDTO(),
                GroupMembers = [new() { MemberRole = 1, Email = profile.Email, Document = profile.Document, AspUserId = profile.AspId }]
            });
        }
        finally
        {
            var fixture = await users.FindByEmailAsync(profile.Email);
            if (fixture is not null)
            {
                await users.RemoveFromRolesAsync(fixture, await users.GetRolesAsync(fixture));
                await users.SetLockoutEnabledAsync(fixture, true);
                await users.SetLockoutEndDateAsync(fixture, DateTimeOffset.MaxValue);
                await users.UpdateSecurityStampAsync(fixture);
            }
        }
    }
}
