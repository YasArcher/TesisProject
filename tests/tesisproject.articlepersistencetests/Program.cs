using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Repositories.Unified.Implementations;
using tesisproject.backend.Services.Unified;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;

var database = "tesis_article_persistence_test_" + Guid.NewGuid().ToString("N");
var services = new ServiceCollection();
services.AddUnifiedDide(new ConfigurationBuilder().Build(), o => o.UseSqlServer(
    $@"Server=.\DINNOVA;Database={database};Integrated Security=True;TrustServerCertificate=True",
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")));
// The application's mixed composition must never make these repositories choose a legacy context.
services.AddScoped<AppDbContext>(_ => throw new InvalidOperationException("Legacy context was resolved"));
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<UnifiedDideDbContext>();
var uow = scope.ServiceProvider.GetRequiredService<IUnifiedUnitOfWork>();
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++; Console.WriteLine("PASS: " + message);
}
try
{
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any() && !db.Database.HasPendingModelChanges(), "Existing migrations and model unchanged");
    Check(ReferenceEquals(uow.ArticleRegistrationMatrices, scope.ServiceProvider.GetRequiredService<IUnifiedArticleRegistrationMatrixRepository>())
        && ReferenceEquals(uow.ArticleReads, scope.ServiceProvider.GetRequiredService<IUnifiedArticleReadRepository>())
        && ReferenceEquals(uow.ArticleRegistration, scope.ServiceProvider.GetRequiredService<IUnifiedArticleRegistrationRepository>())
        && ReferenceEquals(uow.ArticleConfiguration, scope.ServiceProvider.GetRequiredService<IUnifiedArticleConfigurationRepository>()), "Specialized UoW repositories resolve through scoped DI");
    Check(uow.RegistrationMatrixRows is GenericRepository<RegistrationMatrixRow>
        && uow.RegistrationMatrixColumns is GenericRepository<RegistrationMatrixColumn>
        && uow.RegistrationMatrixCells is GenericRepository<RegistrationMatrixCell>
        && uow.ArticleFields is GenericRepository<FieldCatalogEntry>
        && ReferenceEquals(uow.ArticleFields, scope.ServiceProvider.GetRequiredService<IGenericRepository<FieldCatalogEntry>>()),
        "Simple persistence reuses shared GenericRepository and the same DI scope");
    var fields = new[]
    {
        new FieldCatalogEntry { EntityName = "Article", FieldKey = "Title", FieldLabel = "Title", DataType = "string", SourceType = "Physical", IsActive = true, IsVisible = true },
        new FieldCatalogEntry { EntityName = "ArticleParticipant", FieldKey = "Nombre", FieldLabel = "Name", DataType = "string", SourceType = "Physical", IsActive = true, IsVisible = true },
        new FieldCatalogEntry { EntityName = "Article", FieldKey = "Hidden", FieldLabel = "Hidden", DataType = "string", SourceType = "Dynamic", IsActive = true, IsVisible = false }
    };
    await uow.ArticleFields.AddRangeAsync(fields);
    await uow.SaveChangesAsync();
    var selectedIds = fields.Select(f => f.FieldId).ToArray();
    var eligible = await uow.ArticleFields.GetAllAsync(f => selectedIds.Contains(f.FieldId) && f.IsActive && f.IsVisible
        && (f.EntityName == "Article" || f.EntityName == "ArticleParticipant"));
    Check(eligible.Count == 2, "Eligible-field predicates compose through generic filtering; no business policy in repository");
    var matrix = new RegistrationMatrix { Name = "Matrix A", CreatedByUserId = "owner-A", Status = "Draft",
        Columns = [new() { FieldId = fields[0].FieldId, DisplayOrder = 1 }, new() { FieldId = fields[1].FieldId, DisplayOrder = 2 }],
        Rows = [new() { RowNumber = 2 }, new() { RowNumber = 1, Cells = [new() { FieldId = fields[0].FieldId, RawValue = "Draft title" }] }] };
    var second = new RegistrationMatrix { Name = "Matrix B", CreatedByUserId = "owner-B", Status = "Submitted" };
    await uow.ArticleRegistrationMatrices.AddRangeAsync([matrix, second]);
    Check(!await uow.ArticleRegistrationMatrices.ExistsAsync(m => m.Name == matrix.Name), "Repositories stage writes without implicit SaveChanges");
    await uow.SaveChangesAsync();
    var query = uow.ArticleRegistrationMatrices.QueryWithDetails().Where(m => m.CreatedByUserId == "owner-A" && m.Status == "Draft");
    var graph = await query.SingleAsync();
    Check(query.ToQueryString().Contains("WHERE") && graph.Columns.All(c => c.Field is not null)
        && graph.Rows.Select(r => r.RowNumber).SequenceEqual(new[] { 1, 2 }) && graph.Rows.First().Cells.Single().RawValue == "Draft title",
        "Owner/status filters run in SQL and full matrix graph loads in one query");
    Check(!db.Entry(graph).IsKeySet || db.Entry(graph).State == EntityState.Detached, "Detail queries are no-tracking by default");
    var summaries = await uow.ArticleRegistrationMatrices.Query().Where(m => m.CreatedByUserId == "owner-A")
        .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt).ThenByDescending(m => m.RegistrationMatrixId).Take(10)
        .Select(m => new { m.RegistrationMatrixId, ColumnCount = m.Columns.Count, RowCount = m.Rows.Count, m.Status, m.LastImportBatchId }).ToListAsync();
    Check(summaries.Single().ColumnCount == 2 && summaries.Single().RowCount == 2, "Summary counts/order/pagination use generic IQueryable projection");
    var tracked = await uow.ArticleRegistrationMatrices.QueryWithDetails(false).SingleAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId);
    tracked.Name = "Updated matrix"; tracked.Status = "Reviewed"; tracked.LastImportBatchId = 123;
    await uow.SaveChangesAsync();
    Check((await uow.ArticleRegistrationMatrices.FirstOrDefaultAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId))!.Status == "Reviewed",
        "Tracked aggregate updates persist states without enforcing workflow permissions");
    var row = new RegistrationMatrixRow { RegistrationMatrixId = matrix.RegistrationMatrixId, RowNumber = 3 };
    await uow.RegistrationMatrixRows.AddAsync(row); await uow.SaveChangesAsync();
    Check(await uow.RegistrationMatrixRows.GetByIdAsync([row.RegistrationMatrixRowId]) is not null, "Generic row create/GetById");
    var cell = new RegistrationMatrixCell { RegistrationMatrixRowId = row.RegistrationMatrixRowId, FieldId = fields[1].FieldId, RawValue = "Participant snapshot input" };
    await uow.RegistrationMatrixCells.AddAsync(cell); await uow.SaveChangesAsync();
    cell.RawValue = "Updated input"; uow.RegistrationMatrixCells.Update(cell); await uow.SaveChangesAsync();
    Check(await uow.RegistrationMatrixCells.ExistsAsync(c => c.RegistrationMatrixCellId == cell.RegistrationMatrixCellId && c.RawValue == "Updated input"), "Generic cell create/update/exists");
    uow.RegistrationMatrixCells.Remove(cell); await uow.SaveChangesAsync();
    uow.RegistrationMatrixRows.Remove(row); await uow.SaveChangesAsync();
    Check(!await uow.RegistrationMatrixRows.ExistsAsync(r => r.RegistrationMatrixRowId == row.RegistrationMatrixRowId)
        && !await uow.RegistrationMatrixCells.ExistsAsync(c => c.RegistrationMatrixCellId == cell.RegistrationMatrixCellId), "Generic child delete respects FK dependency order");
    var extraColumn = new RegistrationMatrixColumn { RegistrationMatrixId = matrix.RegistrationMatrixId, FieldId = fields[2].FieldId, DisplayOrder = 3 };
    await uow.RegistrationMatrixColumns.AddAsync(extraColumn); await uow.SaveChangesAsync();
    extraColumn.WidthUnits = 2; uow.RegistrationMatrixColumns.Update(extraColumn); await uow.SaveChangesAsync();
    Check((await uow.RegistrationMatrixColumns.GetByIdAsync([extraColumn.RegistrationMatrixColumnId]))!.WidthUnits == 2, "Generic column CRUD");
    uow.RegistrationMatrixColumns.Remove(extraColumn); await uow.SaveChangesAsync();

    // AppUser data lookup is already covered; no new identity abstraction or provisioning flow.
    var identity = new IdentityUser<int> { UserName = "fixture", NormalizedUserName = "FIXTURE" };
    db.Users.Add(identity); await db.SaveChangesAsync();
    var appUser = new AppUser { IdLocal = identity.Id, IdAsp = 8123 };
    await uow.AppUsers.AddAsync(appUser); await uow.SaveChangesAsync();
    Check((await uow.AppUsers.GetByLocalIdAsync(identity.Id))!.IdUser == appUser.IdUser
        && await uow.AppUsers.GetByLocalIdAsync(-1) is null, "Existing AppUser repository covers ArticleUserContext lookup");

    db.ChangeTracker.Clear();
    var beforeRows = await uow.RegistrationMatrixRows.CountAsync();
    try
    {
        await uow.ExecuteInTransactionAsync(async ct =>
        {
            var deleting = await uow.ArticleRegistrationMatrices.QueryWithDetails(false).SingleAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId, ct);
            uow.ArticleRegistrationMatrices.RemoveGraph(deleting);
            await uow.SaveChangesAsync(ct);
            throw new InvalidOperationException("Injected after delete");
            #pragma warning disable CS0162
            return true;
            #pragma warning restore CS0162
        });
    }
    catch (InvalidOperationException ex) when (ex.Message == "Injected after delete") { }
    Check(await uow.ArticleRegistrationMatrices.ExistsAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId)
        && await uow.RegistrationMatrixRows.CountAsync() == beforeRows, "Graph delete participates in UoW rollback");
    var deletingGraph = await uow.ArticleRegistrationMatrices.QueryWithDetails(false).SingleAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId);
    uow.ArticleRegistrationMatrices.RemoveGraph(deletingGraph); await uow.SaveChangesAsync();
    Check(!await uow.ArticleRegistrationMatrices.ExistsAsync(m => m.RegistrationMatrixId == matrix.RegistrationMatrixId)
        && !await uow.RegistrationMatrixRows.ExistsAsync(r => r.RegistrationMatrixId == matrix.RegistrationMatrixId)
        && !await uow.RegistrationMatrixColumns.ExistsAsync(c => c.RegistrationMatrixId == matrix.RegistrationMatrixId)
        && !await uow.RegistrationMatrixCells.Query().AnyAsync()
        && await uow.ArticleRegistrationMatrices.ExistsAsync(m => m.RegistrationMatrixId == second.RegistrationMatrixId)
        && await uow.ArticleFields.CountAsync() == 3, "NoAction graph deletion removes dependents, preserving other matrices and field catalog");
    var unifiedRepositories = typeof(UnifiedArticleRegistrationMatrixRepository).Assembly.GetTypes()
        .Where(t => t.IsClass && t.Namespace == "tesisproject.backend.Repositories.Unified.Implementations");
    Check(unifiedRepositories.All(t => t.GetConstructors().SelectMany(c => c.GetParameters()).All(p =>
        !typeof(DbContext).IsAssignableFrom(p.ParameterType) || p.ParameterType == typeof(UnifiedDideDbContext))), "All Unified repository constructors use only UnifiedDideDbContext");
    Console.WriteLine($"PASS: {checks} Article persistence SQL/EF assertions.");
}
finally
{
    if (!database.StartsWith("tesis_article_persistence_test_", StringComparison.Ordinal)) throw new Exception("Invalid fixture");
    await db.Database.EnsureDeletedAsync();
}
