using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;

if (args.Contains("baseline-source")) { await BaselineSource.InspectAsync(); return; }
if (args.Contains("deployment-test")) { await DeploymentTests.RunAsync(); return; }
if (args.Contains("superadmin-test")) { await SuperadminBootstrapTests.RunAsync(); return; }
if (args.Contains("project-seed-test")) { await ProjectSeedTests.RunAsync(); return; }

if (args.Contains("export-contracts")) { ControllerContractSnapshot.Export(typeof(tesisproject.backend.Controllers.Unified.UnifiedArticlesController).Assembly); return; }

// Explicit operator command: target is validated as local Unified, never DW/legacy.
await using var db = new UnifiedDideDbContextFactory().CreateDbContext(["--environment", "Development"]);
Console.WriteLine("Unified target: " + db.Database.GetDbConnection().DataSource + "/" + db.Database.GetDbConnection().Database);
var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
Console.WriteLine("Pending migrations: " + string.Join(", ", pending));
if (args.Contains("migrate"))
{
    await db.Database.MigrateAsync(); // On a real data conflict, propagate and stop; never repair data.
    Console.WriteLine("Applied: " + string.Join(", ", pending));
}
Console.WriteLine("Migration history: " + string.Join(", ", await db.Database.GetAppliedMigrationsAsync()));
if (args.Contains("runtime")) await RuntimeProbe.RunAsync();
if (args.Contains("inspect"))
{
    Console.WriteLine("Types: " + System.Text.Json.JsonSerializer.Serialize(await db.Set<tesisproject.backend.Data.UnifiedEntities.Catalogs.ProductType>().Select(x => new { x.Id, x.Name }).ToListAsync()));
    Console.WriteLine("Definitions: " + await db.Set<tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAttributeDefinition>().CountAsync());
    Console.WriteLine("Forms: " + System.Text.Json.JsonSerializer.Serialize(await db.Set<tesisproject.backend.Data.UnifiedEntities.Articles.FormDefinition>().Select(x => new { x.FormId, x.FormKey, x.EntityName, x.IsActive }).ToListAsync()));
    Console.WriteLine("Fields: " + await db.Set<tesisproject.backend.Data.UnifiedEntities.Articles.FieldCatalogEntry>().CountAsync());
    Console.WriteLine("SQL counts: " + System.Text.Json.JsonSerializer.Serialize(new {
        Products = await db.Set<tesisproject.backend.Data.UnifiedEntities.Core.Products.Product>().CountAsync(),
        ProductValues = await db.Set<tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductValue>().CountAsync(),
        Articles = await db.Set<tesisproject.backend.Data.UnifiedEntities.Articles.Article>().CountAsync(),
        ProductAuthors = await db.Set<tesisproject.backend.Data.UnifiedEntities.Core.Products.ProductAuthor>().CountAsync(),
        ParticipantDynamicValues = await db.Set<tesisproject.backend.Data.UnifiedEntities.Articles.ProductAuthorDynamicFieldValue>().CountAsync(),
        ArticleReadView = await db.ArticleReads.CountAsync()
    }));
    Console.WriteLine("Projects: " + System.Text.Json.JsonSerializer.Serialize(await db.Set<tesisproject.backend.Data.UnifiedEntities.Core.Project>().Select(x => new { x.ProjectId, x.FacultyId }).Take(3).ToListAsync()));
}
