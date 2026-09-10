using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using tesisproject.backend.Data;
using tesisproject.backend.Data.ReadModels;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Implementations;

if (args.Contains("--hardening"))
{
    await ArticleReadHardeningTests.RunAsync();
    return;
}

var database = "tesis_articles_read_test_" + Guid.NewGuid().ToString("N");
var cs = $@"Server=.\DINNOVA;Database={database};Integrated Security=True;TrustServerCertificate=True";
await using var db = new UnifiedDideDbContext(new DbContextOptionsBuilder<UnifiedDideDbContext>()
    .UseSqlServer(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistoryUnifiedDide", "dbo")).Options);
int checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
async Task Sql(string sql) => await db.Database.ExecuteSqlRawAsync(sql);
async Task RejectSql(string sql, int? number = null)
{
    try { await Sql(sql); }
    catch (SqlException e) when (!number.HasValue || e.Number == number) { checks++; return; }
    throw new Exception("SQL unexpectedly accepted a write/conflict: " + sql);
}

try
{
    var migrations = db.Database.GetMigrations().ToArray();
    var target = migrations.Single(x => x.EndsWith("_AddArticleReadView"));
    var previous = migrations.TakeWhile(x => x != target).Last();
    await db.GetService<IMigrator>().MigrateAsync(previous);
    await Sql("""
        SET IDENTITY_INSERT dbo.ProductTypes ON;
        INSERT dbo.ProductTypes(Id,Name,IsActive,IsLocked) VALUES
          (1,N'PRODUCCIÓN CIENTÍFICA',1,0),(2,N'PRODUCCIÓN REGIONAL',1,0),(3,N'Other',1,0);
        SET IDENTITY_INSERT dbo.ProductTypes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributes ON;
        INSERT dbo.ProductAttributes(Id,Name,IsActive,IsLocked,DataType) VALUES
          (3,N'REVISTA',1,0,0),(4,N'BASE DE DATOS',1,0,0),(5,N'IMPACTO / SJR',1,0,0),
          (6,N'CUARTIL',1,0,0),(7,N'ISSN / ISBN',1,0,0),(8,N'DOI',1,0,0),
          (9,N'AÑO',1,0,0),(10,N'URL',1,0,0);
        SET IDENTITY_INSERT dbo.ProductAttributes OFF;
        SET IDENTITY_INSERT dbo.ProductAttributeDefinitions ON;
        INSERT dbo.ProductAttributeDefinitions(Id,ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT 100+Id,1,Id,0,Id FROM dbo.ProductAttributes;
        INSERT dbo.ProductAttributeDefinitions(Id,ProductTypeId,ProductAttributeId,IsRequired,DisplayOrder)
        SELECT 200+Id,2,Id,0,Id FROM dbo.ProductAttributes;
        SET IDENTITY_INSERT dbo.ProductAttributeDefinitions OFF;
        INSERT dbo.Faculties(Name,IsActive,CreatedAt) VALUES(N'Article faculty',1,SYSUTCDATETIME());
        SET IDENTITY_INSERT dbo.Products ON;
        INSERT dbo.Products(Id,Title,ProductTypeId,IsActive,CreatedAt) VALUES
          (1001,N'Legacy article',1,1,SYSUTCDATETIME()),
          (1002,N'Regional article',2,1,SYSUTCDATETIME()),
          (1003,N'No extension',1,1,SYSUTCDATETIME()),
          (1004,N'Other product',3,1,SYSUTCDATETIME());
        SET IDENTITY_INSERT dbo.Products OFF;
        INSERT dbo.Articles(ProductId,Doi,[Year],PublicationUrl,FacultyId,HasInterculturalComponent,IsOpenAccess)
        SELECT 1001,N'10.legacy',2020,N'https://legacy.test',FacultyId,0,1 FROM dbo.Faculties;
        INSERT dbo.ProductValues(ProductId,AttributeDefinitionId,Value,CreatedAt) VALUES
          (1001,108,N'10.conflict',SYSUTCDATETIME()),
          (1001,106,N'Q1',SYSUTCDATETIME()),
          (1002,203,N'Journal PV',SYSUTCDATETIME()),
          (1002,204,N'Scopus',SYSUTCDATETIME()),
          (1002,205,N'1.234567',SYSUTCDATETIME()),
          (1002,206,N'Q2',SYSUTCDATETIME()),
          (1002,207,N'1234-5678',SYSUTCDATETIME()),
          (1002,208,N'10.regional',SYSUTCDATETIME()),
          (1002,209,N'2025',SYSUTCDATETIME()),
          (1002,210,N'https://regional.test',SYSUTCDATETIME()),
          (1002,108,N'WRONG TYPE',SYSUTCDATETIME());
        """);
    try { await db.GetService<IMigrator>().MigrateAsync(target); throw new Exception("Conflict was silently discarded"); }
    catch (SqlException e) when (e.Number == 51003) { checks++; }
    await Sql("DELETE dbo.ProductValues WHERE ProductId=1001 AND AttributeDefinitionId=108;");
    await db.GetService<IMigrator>().MigrateAsync(target);

    var repo = new UnifiedArticleReadRepository(db);
    var rows = await repo.Query().OrderBy(x => x.ProductId).ToListAsync();
    Check(rows.Count == 3 && rows.Select(x => x.ProductId).Distinct().Count() == 3, "One row per Article Product, including missing extension/values");
    var old = rows.Single(x => x.ProductId == 1001);
    Check(old.Doi == "10.legacy" && old.Year == 2020 && old.PublicationUrl == "https://legacy.test", "Existing attributes transferred before dropping columns");
    var regional = rows.Single(x => x.ProductId == 1002);
    Check(regional.Doi == "10.regional" && regional.Year == 2025 && regional.Quartile == "Q2", "Canonical attributes and matching product-type definitions");
    Check(regional.Journal == "Journal PV" && regional.IndexingDatabase == "Scopus" &&
          regional.Sjr == 1.234567m && regional.Issn == "1234-5678", "All base attributes pivoted");
    Check(rows.Single(x => x.ProductId == 1003).ArticleId is null && regional.FacultyId is null, "Optional structure preserved without implicit faculty");
    Check(await repo.Query().Where(x => x.Quartile == "Q2").CountAsync() == 1, "Quartile filter");
    Check(await repo.Query().Where(x => x.FacultyId == old.FacultyId).CountAsync() == 1, "Explicit faculty filter");
    Check(await repo.Query().Where(x => x.FacultyId == old.FacultyId && x.Quartile == "Q1" && x.Year == 2020).CountAsync() == 1, "Combined faculty/quartile/year filter");
    var composed = repo.Query().Where(x => x.ProductId == 1002 && x.ProjectId == null &&
        x.ProductTypeId == 2 && x.Doi == "10.regional" && x.Title.Contains("Regional"));
    Check(await composed.CountAsync() == 1 && composed.ToQueryString().Contains("WHERE"), "Composable SQL filters without premature materialization");
    await Sql("UPDATE dbo.ProductValues SET Value=N'Q3' WHERE ProductId=1002 AND AttributeDefinitionId=206;");
    Check(await repo.Query().Where(x => x.Quartile == "Q3").CountAsync() == 1, "View reflects canonical writes only");
    await Sql("UPDATE dbo.ProductValues SET Value=N'invalid' WHERE ProductId=1002 AND AttributeDefinitionId=209;");
    regional = await repo.Query().SingleAsync(x => x.ProductId == 1002);
    Check(regional.Year is null && regional.YearRaw == "invalid", "Invalid numeric values preserved as raw text");
    Check(db.Model.FindEntityType(typeof(ArticleReadModel))!.FindPrimaryKey() is null &&
          db.Model.FindEntityType(typeof(ArticleReadModel))!.GetViewName() == "ArticleReadView", "Keyless view mapping");
    foreach (var property in new[] { "Doi", "Year", "PublicationUrl" })
        Check(db.Model.FindEntityType(typeof(Article))!.FindProperty(property) is null, "No duplicate EF property: " + property);
    try { db.Add(new ArticleReadModel()); throw new Exception("Keyless model accepted tracking"); }
    catch (InvalidOperationException) { checks++; }
    await RejectSql("UPDATE dbo.ArticleReadView SET Title=N'forbidden' WHERE ProductId=1001;");
    await RejectSql("DELETE FROM dbo.ArticleReadView WHERE ProductId=1001;");
    await RejectSql("INSERT dbo.ArticleReadView(ProductId,Title,ProductTypeId) VALUES(9000,N'forbidden',1);");
    await RejectSql("UPDATE dbo.Articles SET Doi=N'forbidden';",207);
    await RejectSql("UPDATE dbo.Articles SET [Year]=2026;",207);
    await RejectSql("UPDATE dbo.Articles SET PublicationUrl=N'forbidden';",207);
    Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches model");
    // Exercise rollback and re-apply after restoring a valid year.
    await Sql("UPDATE dbo.ProductValues SET Value=N'2025' WHERE ProductId=1002 AND AttributeDefinitionId=209;");
    await db.GetService<IMigrator>().MigrateAsync(previous);
    await db.GetService<IMigrator>().MigrateAsync(target);
    Check(await repo.Query().CountAsync() == 3, "Migration down/up preserves rows");
    var script = db.GetService<IMigrator>().GenerateScript(previous, target, MigrationsSqlGenerationOptions.Idempotent);
    await db.Database.OpenConnectionAsync();
    foreach (var batch in System.Text.RegularExpressions.Regex.Split(script, @"^GO\s*$",
                 System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        if (!string.IsNullOrWhiteSpace(batch)) await Sql(batch);
    await db.Database.CloseConnectionAsync();
    Check(await repo.Query().CountAsync() == 3, "Idempotent script safely skips an applied migration");
    Console.WriteLine($"PASS: {checks} Article read SQL/EF assertions.");
}
finally
{
    // This unique database is created exclusively by this test invocation.
    if (!database.StartsWith("tesis_articles_read_test_", StringComparison.Ordinal)) throw new Exception("Invalid fixture");
    await db.Database.EnsureDeletedAsync();
}
