using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

// Model contracts run offline; --database additionally validates the separate development database.
using var context = new UnifiedDideDbContextFactory().CreateDbContext([]);
var model = context.GetService<IDesignTimeModel>().Model;
var entities = model.GetEntityTypes().ToArray();
int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
IEntityType Entity<T>() => model.FindEntityType(typeof(T))!;
IForeignKey Fk<T, TPrincipal>(string property) => Entity<T>().GetForeignKeys().Single(f =>
    f.PrincipalEntityType.ClrType == typeof(TPrincipal) && f.Properties.Select(p => p.Name).SequenceEqual([property]));
void Unique<T>(string? filter, params string[] properties) => Check(Entity<T>().GetIndexes().Any(i =>
    i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(properties) && i.GetFilter() == filter),
    $"Missing unique index on {typeof(T).Name} ({string.Join(",", properties)})");

Check(entities.All(e => (e.ClrType.Namespace!.StartsWith("tesisproject.backend.Data.UnifiedEntities.") || e.ClrType.Namespace == "Microsoft.AspNetCore.Identity")), "Original CLR entity leaked into Unified");
Check(entities.All(e => e.GetSchema() == "dbo" && !e.IsTableExcludedFromMigrations()), "Schema isolation or migration ownership failed");
Check(entities.All(e => e.FindPrimaryKey() != null), "Missing PK");
Check(entities.SelectMany(e => e.GetForeignKeys()).All(f => f.DeleteBehavior == DeleteBehavior.NoAction), "Cascade or SetNull delete found");
Check(!entities.Any(e => e.ClrType.Name.Contains("ArticleParticipant")), "Legacy participant/authentication model leaked");
Check(!entities.SelectMany(e => e.GetProperties()).Any(p => p.IsShadowProperty()), "Unexpected shadow columns: " +
    string.Join(", ", entities.SelectMany(e => e.GetProperties()).Where(p => p.IsShadowProperty()).Select(p => $"{p.DeclaringType.Name}.{p.Name}")));
Check(!Fk<Product, Project>("ProjectId").IsRequired, "Independent production requires nullable ProjectId");
Check(Fk<Article, Product>("ProductId") is { IsRequired: true, IsUnique: true }, "Article must specialize exactly one product");
Check(Entity<Article>().FindProperty("ProjectId") == null && Entity<Article>().FindProperty("Title") == null && Entity<Article>().FindProperty("CreatedAt") == null, "Duplicated article product data");
Check(Entity<Product>().FindProperty("Title") is { IsNullable: false }, "Required product title lost");
Check(Fk<Project, Faculty>("FacultyId").IsRequired && !Fk<Article, Faculty>("FacultyId").IsRequired, "Shared faculty nullability changed");
Check(!Fk<Visit, AcademicTerm>("AcademicTermId").IsRequired && !Fk<Article, AcademicTerm>("AcademicTermId").IsRequired, "Shared period relation missing");
Check(Fk<ObjectiveActivityUser, AppUser>("UserId").IsRequired, "Activity user integrity missing");
Check(Fk<GroupMember, AppUser>("UserId").IsRequired && Fk<ExternalResearcherProject, ExternalResearcher>("ExternalResearcherId").IsRequired, "Project participation lost");
Check(Fk<ProductAuthor, Author>("AuthorId").IsRequired, "Authorship source missing");
Check(!Fk<Author, AppUser>("AppUserId").IsRequired && !Fk<Author, ExternalResearcher>("ExternalResearcherId").IsRequired, "Author source FKs must be nullable for XOR");
Check(Entity<Author>().GetCheckConstraints().Single().Sql ==
    "([AppUserId] IS NOT NULL AND [ExternalResearcherId] IS NULL) OR ([AppUserId] IS NULL AND [ExternalResearcherId] IS NOT NULL)", "Incorrect author XOR");
Unique<Article>(null, "ProductId");
Unique<ProductAuthor>(null, "ProductId", "AuthorId");
Unique<ProductAuthor>("[AuthorOrder] IS NOT NULL", "ProductId", "AuthorOrder");
Unique<Author>("[AppUserId] IS NOT NULL", "AppUserId");
Unique<Author>("[ExternalResearcherId] IS NOT NULL", "ExternalResearcherId");
Unique<AppUser>("[IdLocal] IS NOT NULL", "IdLocal");
Unique<AppUser>("[IdAsp] IS NOT NULL", "IdAsp");
Unique<Faculty>("[ExternalFacultyId] IS NOT NULL", "ExternalFacultyId");
Unique<AcademicTerm>("[ExternalPeriodId] IS NOT NULL", "ExternalPeriodId");
Check(Fk<ProductAuthorDynamicFieldValue, ProductAuthor>("ProductAuthorId").IsRequired, "Participant extensions lost");
Check(entities.Count(e => e.ClrType.Name == "IndexingSource") == 1, "Duplicate indexing catalog");

// Build the existing contexts with inert options: adding Unified must not alter their model discovery.
using var originalProjects = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer("Server=127.0.0.1,1;Database=OriginalModelOnly;Integrated Security=True").Options);
using var originalArticles = new tesisproject.backend.Data.Articles.ArticlesDbContext(
    new DbContextOptionsBuilder<tesisproject.backend.Data.Articles.ArticlesDbContext>()
        .UseSqlServer("Server=127.0.0.1,1;Database=OriginalModelOnly;Integrated Security=True").Options);
foreach (var original in new DbContext[] { originalProjects, originalArticles })
    Check(original.Model.GetEntityTypes().All(e => !e.ClrType.Namespace!.Contains("UnifiedEntities") && e.GetSchema() != "Unified"),
        "Unified types leaked into an existing application context");
Check(originalArticles.Model.FindEntityType(typeof(tesisproject.backend.Data.Articles.Entities.ArticleParticipant)) != null,
    "Existing ArticleParticipants must remain available");
var unifiedTypeNames = entities.Select(e => e.ClrType.Name).ToHashSet();
foreach (var oldEntity in originalProjects.Model.GetEntityTypes().Concat(originalArticles.Model.GetEntityTypes()))
{
    var replacement = oldEntity.ClrType.Name switch
    {
        "ArticleParticipant" => "ProductAuthor",
        "ArticleParticipantDynamicFieldValue" => "ProductAuthorDynamicFieldValue",
        var name => name
    };
    Check(unifiedTypeNames.Contains(replacement), $"Operational entity was omitted: {oldEntity.ClrType.Name}");
}

var assembly = context.GetService<IMigrationsAssembly>();
Check(assembly.Migrations.Count == 1, "Unified migration selection leaked other contexts");
Check(!context.Database.HasPendingModelChanges(), "Migration snapshot differs from the final model");
var migration = assembly.CreateMigration(assembly.Migrations.Single().Value, context.Database.ProviderName!);
var tables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
Check(tables.Length == entities.Length, "Migration does not cover all model entities");
Check(migration.UpOperations.All(o => o is EnsureSchemaOperation { Name: "dbo" }
    or CreateTableOperation { Schema: "dbo" } or CreateIndexOperation { Schema: "dbo" }), "Initial migration mutates existing objects");
Check(tables.SelectMany(t => t.ForeignKeys).All(f => f.PrincipalSchema == "dbo" && f.OnDelete == ReferentialAction.NoAction), "Migration FK crosses schema or cascades");
Check(migration.DownOperations.All(o => o is DropTableOperation { Schema: "dbo" }), "Rollback mutates other schemas");
var created = new HashSet<string>();
foreach (var table in tables)
{
    Check(table.ForeignKeys.All(f => f.PrincipalTable == table.Name || created.Contains(f.PrincipalTable)), "Unresolved create-table dependency cycle");
    created.Add(table.Name);
}
var script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
Check(script.Contains("[dbo].[__EFMigrationsHistoryUnifiedDide]"), "Migration history isolation missing");
Check(!script.Contains("[Unified].") && !script.Contains("[DW].") && !script.Contains("DROP TABLE"), "Generated SQL touches existing schemas");

// Exercise change tracking across both author sources without persisting anything.
var project = new Project();
var product = new Product { Project = project, Title = "Unified POC" };
var article = new Article { Product = product };
var internalAuthor = new Author { AppUser = new AppUser { IdAsp = 101 } };
var externalAuthor = new Author { ExternalResearcher = new ExternalResearcher { FullName = "External", Email = "author@example.test" } };
var first = new ProductAuthor { Product = product, Author = internalAuthor, AuthorOrder = 1 };
var second = new ProductAuthor { Product = product, Author = externalAuthor, AuthorOrder = 2 };
context.AddRange(article, first, second);
Check(product.Article == article && product.Authors!.Count == 2 && project.Products.Contains(product), "Unified graph fixup failed");
Check(internalAuthor.Products.Contains(first) && externalAuthor.Products.Contains(second), "Both author sources must support product authorship");

Check(Entity<IdentityUser<int>>().GetTableName() == "AspNetUsers", "Identity is missing");
Check(Fk<AppUser, IdentityUser<int>>("IdLocal") is { IsRequired: false }, "Local identity bridge FK missing");
Check(Fk<RefreshToken, IdentityUser<int>>("UserId").IsRequired, "Refresh token FK missing");
Check(Entity<ProductAuthor>().FindProperty("InstitutionalPersonIdSnapshot") == null &&
    Entity<ProductAuthor>().FindProperty("ExternalAuthorIdSnapshot") == null &&
    Entity<Author>().FindProperty("ExternalAuthorId") != null, "Identity references remain incorrectly attached to participation");
try
{
    UnifiedDideDbContextFactory.ValidateDestination("Server=.\\DINNOVA;Database=tesis;Integrated Security=True", ["Server=.\\DINNOVA;Database=tesis;Integrated Security=True"]);
    throw new Exception("Original database was accepted");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("matches a database")) { checks++; }

Console.WriteLine($"PASS: {checks} model checks; {entities.Length} entities/tables; {entities.Sum(e => e.GetForeignKeys().Count())} FKs; {entities.Sum(e => e.GetIndexes().Count())} indexes.");
if (args.Contains("--database")) await DatabaseValidation.RunAsync();
