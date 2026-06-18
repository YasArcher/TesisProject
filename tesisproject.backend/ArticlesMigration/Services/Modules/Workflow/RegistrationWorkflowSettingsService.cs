using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Services.Modules.Workflow;

public sealed class RegistrationWorkflowSettingsService : IRegistrationWorkflowSettingsService
{
    private const string EntryModeKey = "RegistrationWorkflow.EntryMode";
    private readonly AppDbContext _db;

    public RegistrationWorkflowSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RegistrationWorkflowSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var mode = await ReadEntryModeAsync(ct);
        return BuildDto(mode);
    }

    public async Task<RegistrationWorkflowSettingsDto> UpdateAsync(
        UpdateRegistrationWorkflowSettingsRequest request,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var mode = NormalizeMode(request.EntryMode);
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name;

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
MERGE [dbo].[InstitutionalSetting] AS target
USING (SELECT {EntryModeKey} AS [Key], {mode} AS [Value]) AS source
ON target.[Key] = source.[Key]
WHEN MATCHED THEN
    UPDATE SET [Value] = source.[Value], [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = {userId}
WHEN NOT MATCHED THEN
    INSERT ([Key], [Value], [UpdatedAt], [UpdatedByUserId])
    VALUES (source.[Key], source.[Value], SYSUTCDATETIME(), {userId});", ct);

        return BuildDto(mode);
    }

    public async Task<bool> CanInitiateRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default)
        => await CanInitiateArticleRegistrationAsync(user, ct)
           || await CanInitiateMatrixRegistrationAsync(user, ct);

    public async Task<bool> CanInitiateArticleRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default)
        => await CanInitiateRegistrationAsync(user, AppRoles.ArticleRegistrationUser, ct);

    public async Task<bool> CanInitiateMatrixRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default)
        => await CanInitiateRegistrationAsync(user, AppRoles.RegistrationMatrixUser, ct);

    private async Task<bool> CanInitiateRegistrationAsync(ClaimsPrincipal user, string permissionRole, CancellationToken ct)
    {
        if (user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Analyst))
        {
            return true;
        }

        var mode = await ReadEntryModeAsync(ct);
        var hasExplicitPermission = user.IsInRole(permissionRole);
        var isAuthor = user.IsInRole(AppRoles.Author) || (hasExplicitPermission && mode != RegistrationEntryModes.UodideOnly);
        var isUodide = user.IsInRole(AppRoles.WorkflowReviewerUodide) || (hasExplicitPermission && mode != RegistrationEntryModes.AuthorOnly);

        return mode switch
        {
            RegistrationEntryModes.AuthorOnly => isAuthor,
            RegistrationEntryModes.UodideOnly => isUodide,
            _ => isAuthor || isUodide
        };
    }

    private async Task<string> ReadEntryModeAsync(CancellationToken ct)
    {
        try
        {
            var value = await _db.Database
                .SqlQueryRaw<string>(
                    "SELECT [Value] FROM [dbo].[InstitutionalSetting] WHERE [Key] = {0}",
                    EntryModeKey)
                .FirstOrDefaultAsync(ct);

            return NormalizeMode(value);
        }
        catch
        {
            return RegistrationEntryModes.AuthorAndUodide;
        }
    }

    private static string NormalizeMode(string? mode)
        => RegistrationEntryModes.All.Contains(mode, StringComparer.OrdinalIgnoreCase)
            ? RegistrationEntryModes.All.First(x => string.Equals(x, mode, StringComparison.OrdinalIgnoreCase))
            : RegistrationEntryModes.AuthorAndUodide;

    private static RegistrationWorkflowSettingsDto BuildDto(string mode)
    {
        var option = GetOptions().First(x => x.Value == mode);
        return new RegistrationWorkflowSettingsDto
        {
            EntryMode = option.Value,
            EntryModeLabel = option.Label,
            Description = option.Description,
            EntryModeOptions = GetOptions()
        };
    }

    private static List<RegistrationEntryModeOptionDto> GetOptions() =>
    [
        new()
        {
            Value = RegistrationEntryModes.AuthorAndUodide,
            Label = "Autores y Revisor UODIDE",
            Description = "Autores y Revisor UODIDE pueden iniciar registros; luego UODIDE valida y Área Técnica procesa."
        },
        new()
        {
            Value = RegistrationEntryModes.AuthorOnly,
            Label = "Solo autores",
            Description = "Solo los autores inician registros; UODIDE mantiene su rol de validación inicial."
        },
        new()
        {
            Value = RegistrationEntryModes.UodideOnly,
            Label = "Solo Revisor UODIDE",
            Description = "Los autores no inician registros; UODIDE registra, valida y remite a Área Técnica."
        }
    ];
}
