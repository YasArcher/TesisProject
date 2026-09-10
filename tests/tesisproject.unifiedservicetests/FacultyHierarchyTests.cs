using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using tesisproject.backend.Controllers.Unified;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Migrations.Unified;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified.Implementations;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Faculty;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

internal static class FacultyHierarchyTests
{
    private static ExternalFacultyCareerFlatModel Node(int id, int? parent = null, string? name = null)
        => new() { Id = id, ParentId = parent, Name = name ?? $"Node {id}" };

    public static async Task RunAsync(Action<bool, string> check)
    {
        await using (var f = new SyncFixture())
        {
            f.Faculties = [Node(800, 500), Node(900, 500), Node(500)];
            var first = await f.FacultySync.SynchronizeAsync();
            check(first.Success && first.Data is { Inserted: 3, Updated: 0, Skipped: 0 } && f.Saves == 1, "A/D: child before parent, three inserts, one save");
            var nodes = await f.Context.Set<Faculty>().AsNoTracking().ToDictionaryAsync(x => x.ExternalFacultyId!.Value);
            check(nodes[800].ParentFacultyId == nodes[500].FacultyId && nodes[900].ParentFacultyId == nodes[500].FacultyId, "A: both children persist generated local parent key");
            f.Context.ChangeTracker.Clear();
            var again = await f.FacultySync.SynchronizeAsync();
            check(again.Success && again.Data is { Inserted: 0, Updated: 0, Unchanged: 3 } && f.Saves == 1, "B: identical tree after reload has zero additional saves");
        }
        await using (var f = new SyncFixture())
        {
            var created = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            f.Context.AddRange(new Faculty { FacultyId = 17, ExternalFacultyId = 500, Name = "Node 500" },
                new Faculty { FacultyId = 29, ExternalFacultyId = 800, Name = "Node 800", IsActive = false, CreatedAt = created });
            await f.Context.SaveChangesAsync();
            f.Context.ChangeTracker.Clear();
            f.Faculties = [Node(900, 800), Node(800, 500), Node(500)];
            check((await f.FacultySync.SynchronizeAsync()).Success && f.Saves == 1, "C/E: three levels synced together");
            var child = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 800);
            var grandchild = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 900);
            check(child.FacultyId == 29 && child.ParentFacultyId == 17 && child.ParentFacultyId != 500 && grandchild.ParentFacultyId == 29, "C/E: external 500/800 resolve to local 17/29 over three levels");
            check(!child.IsActive && child.CreatedAt == created && child.LastSyncedAt.HasValue, "N: inactive child and creation metadata retained");
            var oldSync = child.LastSyncedAt;
            f.Faculties = [Node(800, 600), Node(600)];
            var moved = await f.FacultySync.SynchronizeAsync();
            var newParent = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 600);
            check(moved.Success && moved.Data is { Inserted: 1, Updated: 1 } && f.Saves == 2 && child.FacultyId == 29 && child.ParentFacultyId == newParent.FacultyId, "F: existing child moves to new parent in one save");
            check(child.LastSyncedAt >= oldSync && grandchild.ParentFacultyId == 29 && !child.IsActive, "F/O/N: descendants and local metadata unaffected by move");
            f.Faculties = [Node(800)];
            check((await f.FacultySync.SynchronizeAsync()).Data?.Updated == 1 && child.ParentFacultyId is null && child.Parent is null && f.Saves == 3, "G: child becomes root on same local ID");
            check(f.Context.Set<Faculty>().Count() == 4 && f.Context.Set<Faculty>().Single(x => x.FacultyId == 17).IsActive, "O: absent roots and children retained");
            f.Faculties = [Node(950, 500), Node(960, 500, "Same"), Node(970, 500, "Same")];
            var partial = await f.FacultySync.SynchronizeAsync();
            check(partial.Success && partial.Data?.Inserted == 3 && f.Saves == 4, "M/Q/R: omitted local parent and multiple new children accepted");
            check(f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 950).ParentFacultyId == 17 && f.Context.Set<Faculty>().Count(x => x.Name == "Same") == 2, "M/Q/R: same name does not merge nodes; parent resolves local ID");
            f.Failure = true;
            check(!(await f.FacultySync.SynchronizeAsync()).Success && f.Saves == 4 && !f.Context.ChangeTracker.HasChanges(), "P: provider failure has no mutation/save");
            f.Failure = false;
            await ReadsAsync(f, check);
        }
        foreach (var (label, rows) in new (string, List<ExternalFacultyCareerFlatModel>)[]
        {
            ("H missing parent", [Node(1, 999)]), ("I self parent", [Node(1, 1)]),
            ("J two-cycle", [Node(1, 2), Node(2, 1)]),
            ("K three-cycle", [Node(1, 2), Node(2, 3), Node(3, 1)]),
            ("L duplicate", [Node(1), Node(1)]), ("invalid ID", [Node(0)]),
            ("invalid parent", [Node(1, 0)]), ("invalid child name", [Node(1), Node(2, 1, " ")])
        })
        {
            await using var f = new SyncFixture();
            f.Faculties = rows;
            var result = await f.FacultySync.SynchronizeAsync();
            check(!result.Success && result.Error == ErrorType.Validation && f.Saves == 0 && !f.Context.ChangeTracker.HasChanges() && !f.Context.Set<Faculty>().Any(), label + ": whole snapshot rejected before mutation");
        }
        await using (var f = new SyncFixture())
        {
            f.Faculties = [Node(500), Node(800, 500), Node(900, 800)];
            await f.FacultySync.SynchronizeAsync();
            f.Context.ChangeTracker.Clear();
            f.Faculties = [Node(500, 900, "Must not rename")];
            var cycle = await f.FacultySync.SynchronizeAsync();
            check(cycle.ErrorCode == ErrorCodes.CatalogSynchronization.InvalidResponse && f.Saves == 1 && !f.Context.ChangeTracker.HasChanges(), "Cycle through two omitted local ancestors rejected before mutation");
            check(f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 500).Name == "Node 500", "Invalid graph cannot rename an existing node");
            // The proposed graph, rather than the old graph, is authoritative for snapshot nodes.
            f.Faculties = [Node(500, 900), Node(900)];
            check((await f.FacultySync.SynchronizeAsync()).Success && f.Saves == 2, "Simultaneously breaking old edge permits valid reparenting");
        }
        await RollbackAsync(check);
        await using (var f = new SyncFixture())
        {
            f.Faculties = Enumerable.Range(1, 512).Reverse().Select(id => Node(id, id == 1 ? null : id - 1)).ToList();
            check((await f.FacultySync.SynchronizeAsync()).Data?.Inserted == 512 && f.Saves == 1, "512 levels in reverse order: no hardcoded depth or recursive validation");
            var model = f.Context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Faculty))!;
            var fk = model.GetForeignKeys().Single(x => x.Properties.Single().Name == nameof(Faculty.ParentFacultyId));
            check(fk.PrincipalEntityType == model && fk.PrincipalKey.Properties.Single().Name == nameof(Faculty.FacultyId) && !fk.IsRequired, "EF: nullable self FK targets local PK");
            check(fk.DeleteBehavior == DeleteBehavior.NoAction && fk.DependentToPrincipal?.Name == "Parent" && fk.PrincipalToDependent?.Name == "Children", "EF: NoAction and both navigations");
            check(model.GetIndexes().Any(x => x.Properties.Single().Name == "ParentFacultyId" && !x.IsUnique), "EF: parent lookup index");
            var external = model.GetIndexes().Single(x => x.Properties.Single().Name == "ExternalFacultyId");
            check(external.IsUnique && external.GetFilter() == "[ExternalFacultyId] IS NOT NULL", "EF: external ID unique filtered index retained");
        }
        await ControllerAsync(check);
        SqlServerModel(check);
    }

    private static void SqlServerModel(Action<bool, string> check)
    {
        // SQL Server provider and temporary generated keys, entirely offline. This is not a
        // database integration test and does not claim that InMemory enforces foreign keys.
        using var context = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=FacultyHierarchyModelOnly;Integrated Security=True").Options);
        var parent = new Faculty { ExternalFacultyId = 500, Name = "Parent" };
        var child = new Faculty { ExternalFacultyId = 800, Name = "Child" };
        context.AddRange(child, parent);
        child.Parent = parent;
        context.ChangeTracker.DetectChanges();
        var key = context.Entry(parent).Property(x => x.FacultyId);
        var fk = context.Entry(child).Property(x => x.ParentFacultyId);
        check(key.IsTemporary && fk.CurrentValue == key.CurrentValue && fk.CurrentValue != 500, "SQL Server tracking resolves navigation to temporary local parent key before save");
        var migration = new AddFacultyHierarchy();
        check(migration.UpOperations.Count == 3 && migration.UpOperations[0] is AddColumnOperation { Name: "ParentFacultyId", Table: "Faculties", Schema: "dbo", IsNullable: true }, "Migration adds only nullable parent column, index, self FK");
        check(migration.UpOperations[1] is CreateIndexOperation { Table: "Faculties", Name: "IX_Faculties_ParentFacultyId" } index && index.Columns.SequenceEqual(["ParentFacultyId"]), "Migration parent index columns");
        check(migration.UpOperations[2] is AddForeignKeyOperation { Table: "Faculties", PrincipalTable: "Faculties", PrincipalSchema: "dbo", OnDelete: ReferentialAction.NoAction } relation && relation.Columns.SequenceEqual(["ParentFacultyId"]) && relation.PrincipalColumns!.SequenceEqual(["FacultyId"]), "Migration uses local self FK with NoAction");
        check(migration.DownOperations.Count == 3 && migration.DownOperations[0] is DropForeignKeyOperation && migration.DownOperations[1] is DropIndexOperation && migration.DownOperations[2] is DropColumnOperation { Name: "ParentFacultyId" }, "Migration rollback removes only hierarchy additions");
    }

    private static async Task RollbackAsync(Action<bool, string> check)
    {
        foreach (var cancel in new[] { false, true })
        {
            await using var f = new SyncFixture();
            using var cts = new CancellationTokenSource();
            f.Token = cts.Token;
            f.Faculties = [Node(500), Node(800, 500), Node(600)];
            await f.FacultySync.SynchronizeAsync(cts.Token);
            var parent = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 500);
            var child = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 800);
            var other = f.Context.Set<Faculty>().Single(x => x.ExternalFacultyId == 600);
            var oldStamp = child.LastSyncedAt;
            f.Faculties = [Node(800, 600, "Failed rename"), Node(700, 500), Node(1000), Node(600, 1000)];
            f.FailSave = !cancel;
            if (cancel) f.CancelOnAdd = cts.Cancel;
            try
            {
                var failed = await f.FacultySync.SynchronizeAsync(cts.Token);
                check(!cancel && failed.ErrorCode == ErrorCodes.CatalogSynchronization.PersistenceFailed, "Save failure reported");
            }
            catch (OperationCanceledException) { check(cancel, "Caller cancellation propagates"); }
            check(!f.Context.ChangeTracker.HasChanges() && f.Context.Set<Faculty>().Count() == 3, "Failure restores tracking and detaches all new nodes");
            check(child.Parent == parent && child.ParentFacultyId == parent.FacultyId && parent.Children.Count == 1 && parent.Children.Contains(child) && other.Children.Count == 0 && other.Parent is null, $"Failure restores navigations: childParent={child.Parent?.ExternalFacultyId}, FK={child.ParentFacultyId}, oldChildren={string.Join(",", parent.Children.Select(x => x.ExternalFacultyId))}, otherChildren={other.Children.Count}, otherParent={other.Parent?.ExternalFacultyId}");
            check(child.Name == "Node 800" && child.LastSyncedAt == oldStamp, "Failure restores scalar metadata");
            check(await f.Context.SaveChangesAsync() == 0 && f.Context.Set<Faculty>().AsNoTracking().Count() == 3, "Later save cannot resurrect failed inserts through navigation");
            if (!cancel)
            {
                f.FailSave = false;
                check((await f.FacultySync.SynchronizeAsync(cts.Token)).Success && f.Context.Set<Faculty>().Count() == 5, "Same scope retry succeeds after hierarchy rollback");
            }
        }
    }

    private static async Task ReadsAsync(SyncFixture f, Action<bool, string> check)
    {
        f.Context.ChangeTracker.Clear();
        var repo = new UnifiedFacultyRepository(f.Context);
        var uow = Stub.For<IUnifiedUnitOfWork>((method, _) => method.Name == "get_Faculties" ? repo : throw new InvalidOperationException(method.Name));
        var query = new UnifiedFacultyQueryService(uow);
        var before = f.Saves;
        var all = await query.GetHierarchyAsync();
        var roots = await query.GetRootsAsync();
        var children = await query.GetChildrenAsync(17);
        check(all.Success && all.Data!.Count == 7 && all.Data!.Any(x => x.FacultyId == 29 && !x.IsActive), "Local full hierarchy includes inactive nodes and all levels");
        check(roots.Data!.All(x => x.ParentFacultyId is null) && roots.Data!.Count == 3, "Local roots use nullable local FK");
        check(children.Data!.Count == 3 && children.Data!.All(x => x.ParentFacultyId == 17), "Children lookup uses local 17, not external 500");
        check((await query.GetChildrenAsync(500)).Data!.Count == 0, "Unknown/leaf parent yields empty children");
        check(f.Saves == before && !f.Context.ChangeTracker.Entries<Faculty>().Any(), "Queries neither track nor save");
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var cancelled = false;
        try { await query.GetHierarchyAsync(cts.Token); } catch (OperationCanceledException) { cancelled = true; }
        check(cancelled, "Local query propagates cancellation");
    }

    private static async Task ControllerAsync(Action<bool, string> check)
    {
        using var cts = new CancellationTokenSource();
        string? called = null;
        var expected = ServiceResult<List<FacultyHierarchyNodeDTO>>.Ok([]);
        var service = Stub.For<IUnifiedFacultyQueryService>((method, args) =>
        {
            called = method.Name;
            check((CancellationToken)args[^1]! == cts.Token, "Read controller forwards cancellation");
            if (method.Name == "GetChildrenAsync") check((int)args[0]! == 17, "Read controller forwards local parent ID");
            return Task.FromResult(expected);
        });
        var controller = new UnifiedFacultiesController(service);
        var result = await controller.GetHierarchy(cts.Token);
        check(called == "GetHierarchyAsync" && result.Result is OkObjectResult ok && ReferenceEquals(ok.Value, expected), "Hierarchy controller relays service result");
        await controller.GetRoots(cts.Token); check(called == "GetRootsAsync", "Roots controller route");
        await controller.GetChildren(17, cts.Token); check(called == "GetChildrenAsync", "Children controller route");
        var type = typeof(UnifiedFacultiesController);
        check(type.GetCustomAttribute<NonControllerAttribute>() is null && type.GetCustomAttribute<AuthorizeAttribute>()?.Roles == "superadmin" && type.GetCustomAttribute<RouteAttribute>()?.Template == "api/faculties", "Read controller active, admin-only, natural local route");
        var manager = new ApplicationPartManager(); manager.ApplicationParts.Add(new AssemblyPart(type.Assembly)); manager.FeatureProviders.Add(new ControllerFeatureProvider());
        var feature = new ControllerFeature(); manager.PopulateFeature(feature);
        check(feature.Controllers.Any(x => x.AsType() == type), "Read controller active in runtime MVC discovery");
    }
}
