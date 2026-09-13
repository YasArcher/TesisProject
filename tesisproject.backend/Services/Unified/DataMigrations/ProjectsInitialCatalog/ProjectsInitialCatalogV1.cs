using tesisproject.backend.Data;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Implementations.DataMigrations;
using tesisproject.backend.Services.Unified.Interfaces;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Services.Unified.DataMigrations.ProjectsInitialCatalog;

public sealed class ProjectsInitialCatalogV1(UnifiedDideDbContext context) : IDataMigration
{
    public string Code => OperationCodes.ProjectsInitialCatalogV1;
    public string Description => "Catálogos y configuración inicial canónica de Projects.";
    public string Version => "1";
    public int Order => 100;

    public async Task<object?> ApplyAsync(CancellationToken ct = default)
    {
        var updated = await CorrectKnownSmokeFixturesAsync(ct);
        var sections = new List<ProjectsSeedSectionResult>(ProjectsSeedData.Tables.Count);
        foreach (var table in ProjectsSeedData.Tables)
            sections.Add(await table.ApplyAsync(context, ct));
        return new ProjectsInitialCatalogResult(
            sections.Sum(x => x.Inserted),
            updated,
            sections.Sum(x => x.Unchanged),
            sections.ToDictionary(x => x.Table, x => x.Total, StringComparer.Ordinal));
    }

    private async Task<int> CorrectKnownSmokeFixturesAsync(CancellationToken ct)
    {
        var updated = 0;
        var groupType = await context.GroupTypes.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (groupType is { Name: "Integrantes", IsActive: true, IsLocked: true })
        {
            if (await context.Groups.AnyAsync(x => x.GroupTypeId == 1 &&
                    !x.Name.StartsWith("CA-") && !x.Name.StartsWith("CT-"), ct))
                throw new InvalidOperationException("Known GroupTypes fixture has non-smoke references.");
            groupType.IsLocked = false; updated++;
        }

        var convocation = await context.Convocations.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (convocation is { Name: "Cutover smoke", Code: "CT", IsActive: true, IsLocked: false })
        {
            await ProtectProjectsAsync(x => x.ConvocationId == 1, "Convocations", ct);
            convocation.Name = "SIN CONVOCATORIA"; convocation.Code = null; updated++;
        }

        var state = await context.ProjectStates.SingleOrDefaultAsync(x => x.Id == 3, ct);
        if (state is { Name: "En ejecución", IsActive: true, IsLocked: true })
        {
            await ProtectProjectsAsync(x => x.ProjectStateId == 3, "ProjectStates", ct);
            state.Name = "EN EJECUCION"; updated++;
        }

        var projectType = await context.ProjectTypes.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (projectType is { Name: "Investigación", IsActive: true, IsLocked: true })
        {
            await ProtectProjectsAsync(x => x.ProjectTypeId == 1, "ProjectTypes", ct);
            projectType.Name = "Aplicada"; updated++;
        }

        var coordinator = await context.MemberRoleTypes.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (coordinator is { Name: "Coordinador", IsActive: true, Flag: 1, IsLocked: true })
        {
            await ProtectMemberRoleAsync(1, ct);
            coordinator.Name = "Coordinador Principal"; updated++;
        }
        var researcher = await context.MemberRoleTypes.SingleOrDefaultAsync(x => x.Id == 3, ct);
        if (researcher is { Name: "Investigador", IsActive: true, Flag: 1, IsLocked: true })
        {
            await ProtectMemberRoleAsync(3, ct);
            researcher.IsLocked = false; updated++;
        }

        var productType = await context.ProductTypes.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (productType is { Name: "Producto smoke", IsActive: true, IsLocked: false })
        {
            if (await context.Products.AnyAsync(x => x.ProductTypeId == 1 &&
                    (!x.Title.StartsWith("CT-") || x.ProjectId != null), ct))
                throw new InvalidOperationException("Known ProductTypes fixture has non-smoke references.");
            productType.Name = "PRODUCCIÓN CIENTÍFICA"; updated++;
        }

        if (updated > 0) await context.SaveChangesAsync(ct);
        return updated;
    }

    private async Task ProtectProjectsAsync(System.Linq.Expressions.Expression<Func<Project, bool>> predicate,
        string table, CancellationToken ct)
    {
        if (await context.Projects.Where(predicate).AnyAsync(x =>
                !x.ProjectCode.StartsWith("CA-") && !x.ProjectCode.StartsWith("CT-"), ct))
            throw new InvalidOperationException($"Known {table} fixture has non-smoke references.");
    }

    private async Task ProtectMemberRoleAsync(int roleId, CancellationToken ct)
    {
        if (await context.GroupMembers.Where(x => x.MemberRoleId == roleId)
                .AnyAsync(x => !x.Group.Name.StartsWith("CA-") && !x.Group.Name.StartsWith("CT-"), ct))
            throw new InvalidOperationException("Known MemberRoleTypes fixture has non-smoke references.");
    }
}

public sealed record ProjectsInitialCatalogResult(
    int Inserted,
    int Updated,
    int Unchanged,
    IReadOnlyDictionary<string, int> Sections);
